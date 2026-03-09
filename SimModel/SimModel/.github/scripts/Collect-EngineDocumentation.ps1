[CmdletBinding()]
param(
    [string]$EngineRoot,
    [string]$OutputPath,
    [switch]$PassThru
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $resolvedBase = Resolve-FullPath -Path $BasePath
    $resolvedTarget = Resolve-FullPath -Path $TargetPath

    if (-not (Get-Item -LiteralPath $resolvedBase).PSIsContainer)
    {
        $resolvedBase = Split-Path -Parent $resolvedBase
    }

    if (-not $resolvedBase.EndsWith([System.IO.Path]::DirectorySeparatorChar))
    {
        $resolvedBase += [System.IO.Path]::DirectorySeparatorChar
    }

    $baseUri = New-Object System.Uri($resolvedBase)
    $targetUri = New-Object System.Uri($resolvedTarget)
    $relativeUri = $baseUri.MakeRelativeUri($targetUri)

    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('\', '/').Replace('%5C', '/')
}

function Get-XmlAttributeValue {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$Node,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ($null -eq $Node.Attributes)
    {
        return $null
    }

    $attribute = $Node.Attributes[$Name]
    if ($null -eq $attribute)
    {
        return $null
    }

    return $attribute.Value
}

function Normalize-Cref {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Cref
    )

    $normalized = $Cref -replace '^[A-Z]:', ''
    $normalized = $normalized -replace '^global::', ''
    $normalized = $normalized.Replace('{', '<').Replace('}', '>')

    return $normalized.Trim()
}

function Normalize-Markdown {
    param(
        [AllowEmptyString()]
        [string]$Text
    )

    $normalized = $Text -replace "`r", ''
    $normalized = $normalized -replace "[ \t]+`n", "`n"
    $normalized = $normalized -replace "`n{3,}", "`n`n"
    $normalized = $normalized -replace ' {2,}', ' '

    return $normalized.Trim()
}

function Render-XmlNodes {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNodeList]$Nodes
    )

    $parts = New-Object System.Collections.Generic.List[string]

    foreach ($node in $Nodes)
    {
        $parts.Add((Render-XmlNode -Node $node))
    }

    return ($parts -join '')
}

function Render-XmlList {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$Node
    )

    $listType = Get-XmlAttributeValue -Node $Node -Name 'type'
    $items = @($Node.SelectNodes('item'))
    if ($items.Count -eq 0)
    {
        return ''
    }

    $lines = New-Object System.Collections.Generic.List[string]
    for ($index = 0; $index -lt $items.Count; $index++)
    {
        $item = $items[$index]
        $termNode = $item.SelectSingleNode('term')
        $descriptionNode = $item.SelectSingleNode('description')

        $termText = ''
        if ($null -ne $termNode)
        {
            $termText = (Normalize-Markdown -Text (Render-XmlNodes -Nodes $termNode.ChildNodes))
        }

        $descriptionText = ''
        if ($null -ne $descriptionNode)
        {
            $descriptionText = (Normalize-Markdown -Text (Render-XmlNodes -Nodes $descriptionNode.ChildNodes))
        }
        else
        {
            $descriptionText = (Normalize-Markdown -Text (Render-XmlNodes -Nodes $item.ChildNodes))
        }

        if ([string]::IsNullOrWhiteSpace($termText) -and [string]::IsNullOrWhiteSpace($descriptionText))
        {
            continue
        }

        $body = if (-not [string]::IsNullOrWhiteSpace($termText) -and -not [string]::IsNullOrWhiteSpace($descriptionText))
        {
            '**' + $termText + '** — ' + $descriptionText
        }
        elseif (-not [string]::IsNullOrWhiteSpace($descriptionText))
        {
            $descriptionText
        }
        else
        {
            $termText
        }

        $prefix = if ($listType -eq 'number') { '{0}. ' -f ($index + 1) } else { '- ' }
        $lines.Add($prefix + $body)
    }

    if ($lines.Count -eq 0)
    {
        return ''
    }

    return "`n`n$($lines -join "`n")`n`n"
}

function Render-XmlNode {
    param(
        [Parameter(Mandatory = $true)]
        [System.Xml.XmlNode]$Node
    )

    switch ($Node.NodeType)
    {
        ([System.Xml.XmlNodeType]::Text) { return $Node.InnerText }
        ([System.Xml.XmlNodeType]::CDATA) { return $Node.InnerText }
        ([System.Xml.XmlNodeType]::Whitespace) { return $Node.InnerText }
        ([System.Xml.XmlNodeType]::SignificantWhitespace) { return $Node.InnerText }
        ([System.Xml.XmlNodeType]::Element)
        {
            switch ($Node.Name)
            {
                'summary' { return (Render-XmlNodes -Nodes $Node.ChildNodes) }
                'para'
                {
                    $text = (Normalize-Markdown -Text (Render-XmlNodes -Nodes $Node.ChildNodes))
                    if ([string]::IsNullOrWhiteSpace($text))
                    {
                        return ''
                    }

                    return "`n`n$text`n`n"
                }
                'see'
                {
                    $cref = Get-XmlAttributeValue -Node $Node -Name 'cref'
                    if (-not [string]::IsNullOrWhiteSpace($cref))
                    {
                        return '`' + (Normalize-Cref -Cref $cref) + '`'
                    }

                    $langword = Get-XmlAttributeValue -Node $Node -Name 'langword'
                    if (-not [string]::IsNullOrWhiteSpace($langword))
                    {
                        return '`' + $langword + '`'
                    }

                    return $Node.InnerText
                }
                'paramref'
                {
                    $name = Get-XmlAttributeValue -Node $Node -Name 'name'
                    if ([string]::IsNullOrWhiteSpace($name))
                    {
                        return $Node.InnerText
                    }

                    return '`' + $name + '`'
                }
                'typeparamref'
                {
                    $name = Get-XmlAttributeValue -Node $Node -Name 'name'
                    if ([string]::IsNullOrWhiteSpace($name))
                    {
                        return $Node.InnerText
                    }

                    return '`' + $name + '`'
                }
                'c'
                {
                    $text = (Normalize-Markdown -Text (Render-XmlNodes -Nodes $Node.ChildNodes))
                    if ([string]::IsNullOrWhiteSpace($text))
                    {
                        return ''
                    }

                    return '`' + $text + '`'
                }
                'code'
                {
                    $text = $Node.InnerText.Trim()
                    if ([string]::IsNullOrWhiteSpace($text))
                    {
                        return ''
                    }

                    return "`n" + '```csharp' + "`n$text`n" + '```'
                }
                'list' { return (Render-XmlList -Node $Node) }
                'br' { return "`n" }
                'inheritdoc'
                {
                    $cref = Get-XmlAttributeValue -Node $Node -Name 'cref'
                    if ([string]::IsNullOrWhiteSpace($cref))
                    {
                        return 'Inherits documentation from the implemented or overridden member.'
                    }

                    return 'Inherits documentation from `' + (Normalize-Cref -Cref $cref) + '`.'
                }
                default { return (Render-XmlNodes -Nodes $Node.ChildNodes) }
            }
        }
        default { return '' }
    }
}

function Convert-XmlDocCommentToMarkdown {
    param(
        [AllowEmptyString()]
        [string[]]$DocLines
    )

    $cleanLines = foreach ($line in $DocLines)
    {
        $line -replace '^\s*///\s?', ''
    }

    $xmlText = "<root>`n$($cleanLines -join "`n")`n</root>"

    try
    {
        $xml = New-Object System.Xml.XmlDocument
        $xml.LoadXml($xmlText)
    }
    catch
    {
        $fallback = (($cleanLines -join ' ') -replace '<[^>]+>', ' ')
        return (($fallback -replace '\s+', ' ').Trim())
    }

    $inheritdocNode = $xml.DocumentElement.SelectSingleNode('inheritdoc')
    if ($null -ne $inheritdocNode)
    {
        return (Normalize-Markdown -Text (Render-XmlNode -Node $inheritdocNode))
    }

    $summaryNode = $xml.DocumentElement.SelectSingleNode('summary')
    if ($null -eq $summaryNode)
    {
        return ''
    }

    return (Normalize-Markdown -Text (Render-XmlNodes -Nodes $summaryNode.ChildNodes))
}

function Get-DeclarationText {
    param(
        [AllowEmptyString()]
        [string[]]$Lines,

        [Parameter(Mandatory = $true)]
        [int]$StartIndex
    )

    $buffer = New-Object System.Collections.Generic.List[string]
    $started = $false

    for ($index = $StartIndex; $index -lt $Lines.Length -and $buffer.Count -lt 8; $index++)
    {
        $trimmed = $Lines[$index].Trim()
        if (-not $started)
        {
            if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('[') -or $trimmed.StartsWith('#'))
            {
                continue
            }

            $started = $true
        }

        if ([string]::IsNullOrWhiteSpace($trimmed))
        {
            if ($buffer.Count -gt 0)
            {
                break
            }

            continue
        }

        $buffer.Add($trimmed)

        if ($trimmed.Contains('=>') -or $trimmed.EndsWith(';') -or $trimmed.EndsWith('{'))
        {
            break
        }
    }

    if ($buffer.Count -eq 0)
    {
        return ''
    }

    $declaration = ($buffer -join ' ')
    $declaration = $declaration -replace '\s+', ' '
    $declaration = $declaration -replace '\s*\{$', ''

    return $declaration.Trim()
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

if ([string]::IsNullOrWhiteSpace($EngineRoot))
{
    $EngineRoot = Join-Path $repoRoot 'Engine'
}

if ([string]::IsNullOrWhiteSpace($OutputPath))
{
    $OutputPath = Join-Path $repoRoot '.github\agents\documentation-context.generated.md'
}

$EngineRoot = Resolve-FullPath -Path $EngineRoot
$OutputPath = Resolve-FullPath -Path $OutputPath

if (-not (Test-Path -LiteralPath $EngineRoot -PathType Container))
{
    throw "Engine directory not found: $EngineRoot"
}

$documentationMdPath = Join-Path $EngineRoot 'Documentation.md'
$indexMdPath = Join-Path $EngineRoot 'index.md'
$tocYamlPath = Join-Path $EngineRoot 'toc.yml'
$docfxPath = Join-Path $EngineRoot 'docfx.json'

if (-not (Test-Path -LiteralPath $documentationMdPath -PathType Leaf))
{
    throw "Documentation file not found: $documentationMdPath"
}

$sourceFiles = Get-ChildItem -Path $EngineRoot -Filter '*.cs' -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Sort-Object FullName

$fileGroups = New-Object System.Collections.Generic.List[object]
$totalSummaries = 0

foreach ($sourceFile in $sourceFiles)
{
    $lines = [System.IO.File]::ReadAllLines($sourceFile.FullName)
    $entries = New-Object System.Collections.Generic.List[object]

    for ($lineIndex = 0; $lineIndex -lt $lines.Length; $lineIndex++)
    {
        $trimmed = $lines[$lineIndex].TrimStart()
        if (-not $trimmed.StartsWith('///'))
        {
            continue
        }

        $docBlock = New-Object System.Collections.Generic.List[string]
        $docStartLine = $lineIndex + 1

        while ($lineIndex -lt $lines.Length -and $lines[$lineIndex].TrimStart().StartsWith('///'))
        {
            $docBlock.Add($lines[$lineIndex])
            $lineIndex++
        }

        $lineIndex--

        $docBlockText = $docBlock -join "`n"
        if ($docBlockText -notmatch '<summary\b' -and $docBlockText -notmatch '<inheritdoc\b')
        {
            continue
        }

        $declaration = Get-DeclarationText -Lines $lines -StartIndex ($lineIndex + 1)
        if ([string]::IsNullOrWhiteSpace($declaration))
        {
            continue
        }

        $summary = Convert-XmlDocCommentToMarkdown -DocLines $docBlock.ToArray()
        if ([string]::IsNullOrWhiteSpace($summary))
        {
            continue
        }

        $entries.Add([pscustomobject]@{
            Line        = $docStartLine
            Declaration = $declaration
            Summary     = $summary
        })
    }

    if ($entries.Count -eq 0)
    {
        continue
    }

    $relativePath = Get-RelativePath -BasePath $repoRoot -TargetPath $sourceFile.FullName

    $fileGroups.Add([pscustomobject]@{
        RelativePath = $relativePath
        Entries      = $entries
    })

    $totalSummaries += $entries.Count
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory -PathType Container))
{
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$builder = New-Object System.Text.StringBuilder
[void]$builder.AppendLine('# Engine documentation context')
[void]$builder.AppendLine()
[void]$builder.AppendLine('> Generated by `.github/scripts/Collect-EngineDocumentation.ps1`. Do not edit this file manually.')
[void]$builder.AppendLine()
[void]$builder.AppendLine('- Generated at: ' + [DateTime]::UtcNow.ToString('u'))
[void]$builder.AppendLine('- Engine root: `' + (Get-RelativePath -BasePath $repoRoot -TargetPath $EngineRoot) + '`')
[void]$builder.AppendLine('- Conceptual guide: `Engine/Documentation.md`')
[void]$builder.AppendLine('- XML summary source files scanned: ' + $sourceFiles.Count)
[void]$builder.AppendLine('- XML summary blocks collected: ' + $totalSummaries)
[void]$builder.AppendLine()

[void]$builder.AppendLine('## Conceptual documentation')
[void]$builder.AppendLine()
[void]$builder.AppendLine([System.IO.File]::ReadAllText($documentationMdPath))
[void]$builder.AppendLine()

[void]$builder.AppendLine('## Supporting documentation files')
[void]$builder.AppendLine()

if (Test-Path -LiteralPath $indexMdPath -PathType Leaf)
{
    [void]$builder.AppendLine('### Engine/index.md')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine([System.IO.File]::ReadAllText($indexMdPath))
    [void]$builder.AppendLine()
}

if (Test-Path -LiteralPath $tocYamlPath -PathType Leaf)
{
    [void]$builder.AppendLine('### Engine/toc.yml')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('```yaml')
    [void]$builder.AppendLine([System.IO.File]::ReadAllText($tocYamlPath).TrimEnd())
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine()
}

if (Test-Path -LiteralPath $docfxPath -PathType Leaf)
{
    [void]$builder.AppendLine('### Engine/docfx.json')
    [void]$builder.AppendLine()
    [void]$builder.AppendLine('```json')
    [void]$builder.AppendLine([System.IO.File]::ReadAllText($docfxPath).TrimEnd())
    [void]$builder.AppendLine('```')
    [void]$builder.AppendLine()
}

[void]$builder.AppendLine('## XML summary inventory')
[void]$builder.AppendLine()
[void]$builder.AppendLine('| File | Summary blocks |')
[void]$builder.AppendLine('| --- | ---: |')

foreach ($group in $fileGroups)
{
    [void]$builder.AppendLine('| `' + $group.RelativePath + '` | ' + $group.Entries.Count + ' |')
}

[void]$builder.AppendLine()
[void]$builder.AppendLine('## XML summary details')
[void]$builder.AppendLine()

foreach ($group in $fileGroups)
{
    [void]$builder.AppendLine('### ' + $group.RelativePath)
    [void]$builder.AppendLine()

    for ($entryIndex = 0; $entryIndex -lt $group.Entries.Count; $entryIndex++)
    {
        $entry = $group.Entries[$entryIndex]
        [void]$builder.AppendLine('#### Entry ' + ($entryIndex + 1))
        [void]$builder.AppendLine()
        [void]$builder.AppendLine('- Source: `' + $group.RelativePath + ':' + $entry.Line + '`')
        [void]$builder.AppendLine('- Declaration: `' + $entry.Declaration + '`')
        [void]$builder.AppendLine()
        [void]$builder.AppendLine($entry.Summary)
        [void]$builder.AppendLine()
    }
}

$encoding = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($OutputPath, $builder.ToString(), $encoding)

$message = 'Generated documentation context: ' + $OutputPath
Write-Output $message

if ($PassThru)
{
    Write-Output ''
    Write-Output $builder.ToString()
}