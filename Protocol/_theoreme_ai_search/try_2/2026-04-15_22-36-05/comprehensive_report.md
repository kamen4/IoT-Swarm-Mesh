# Комплексный отчет по параметрическому исследованию

Отчет собран автоматически из результатов пакетного прогона и включает: краткую теорию, методику, результаты по топологиям и классам, чувствительность параметров, корреляции, графики и выводы.

## Где лежат артефакты

- Полный результат прогона: [batch_report.json](batch_report.json)
- Компактные данные для графиков: [report_data.json](report_data.json)
- Короткая сводка (раньше генерировалась отдельно): [run_summary.md](run_summary.md)
- Комплексный анализ (машиночитаемый): [comprehensive_analysis.json](comprehensive_analysis.json)
- Лучшие параметры по классам: [best_parameters_by_class.json](best_parameters_by_class.json)
- Корреляции (все): [all_correlations.csv](all_correlations.csv)
- Корреляции параметр-параметр: [pairwise_parameter_correlations.csv](pairwise_parameter_correlations.csv)
- Худшие/нестабильные seed-run: [worst_unstable_runs.csv](worst_unstable_runs.csv)

## Главное: зачем это и что получилось

Цель симуляции — показать, что протокол может выходить в режим, где проверки теоремы (A5/A6/A7 и Леммы 4.1–4.3) выполняются устойчиво, и подобрать **один практический baseline параметров**, который можно брать как стартовый для разных сетей (размер/плотность).

Что важно из этого прогона:

- Лучшая сеть (best-case в этой выборке): **N=18, R=210**, Score=100.00, StableRatio=100.00%, Verdict=STABLE.
- Риск ‘хвоста’: нестабильные/осциллирующие запуски = **5.76%** (48800 оценок).
- Конкретные worst-кейсы (что ломается и где): [worst_unstable_runs.csv](worst_unstable_runs.csv).
- Сходимость проверок (по лучшим найденным параметрам на каждой топологии): `SustainedAllChecks` медиана≈1, P90≈2 (макс=14 на 18x120).
- Ключевые параметры, которые реально ‘двигают’ качество (по `ImpactIndex`): `decayIntervalSteps`, `chargeSpreadFactor`, `switchHysteresisRatio`.
- Математическое обоснование (почему A5/A6/A7 ⇒ дерево): [../../../_docs_v1.0/math/theorem.md](../../../_docs_v1.0/math/theorem.md).

Рекомендуемый baseline (один профиль, который можно брать как стартовый):

```json
{
  "qForward": 194.0,
  "deliveryProbability": 0.61,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 28.0,
  "switchHysteresis": 9.0,
  "switchHysteresisRatio": 0.07,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.28,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.13,
  "linkMemory": 0.912,
  "linkLearningRate": 0.23,
  "linkBonusMax": 60.0
}
```

Если нужно упростить и ‘убрать лишние ручки’: 

- в первую очередь тюнить: `decayIntervalSteps`, `chargeSpreadFactor`, `switchHysteresisRatio`;
- остальные параметры можно **зафиксировать** на baseline до тех пор, пока не появится конкретная причина их трогать.
- параметры с минимальным влиянием в этой выборке (кандидаты на удаление из оптимизации): `linkBonusMax`.

## 1) Короткая теория (что симулируем)

Симулятор моделирует сеть устройств и gateway (корень), где у узлов есть заряд `q_total`. Узел становится *eligible*, когда `q_total ≥ qForward`, и только eligible-узлы участвуют в построении родительского дерева.

В каждом раунде (упрощенно) выполняется:

1. **DOWN**: распространение информации/заряда от gateway, измеряем покрытие и дубли.
2. **UP**: попытки устройств пробиться к gateway по более ‘заряженным’ соседям.
3. **Learning/Spread**: обновление оценок соседей + смешивание зарядов к лучшим оценкам.
4. **Tree rebuild**: выбор родителей (с гистерезисом и штрафами качества/бонусами стабильности).
5. **Decay (опционально)**: периодическое затухание зарядов и памяти линков.

Проверки теоремы в симуляции: аксиомы A5/A6/A7 и леммы 4.1/4.2/4.3. Если eligible-набор пуст (кроме gateway), состояние проверки считается `pending` (не провал/не успех).

### Что означают метрики (которые мы анализируем)

- `Score`: эвристика 0..100 на основе pass-rate, покрытия, дублей и стабильности дерева.
- `TheoremPassRate`: доля раундов, где агрегированная теорема проходит.
- `AssumptionsPassRate`: доля раундов, где аксиомы проходят.
- `CoverageAvg`: среднее покрытие DOWN.
- `DuplicateDrop`: насколько дубли DOWN уменьшаются от начала к концу.
- `EligibleTailRatio`: устойчивость eligible-набора в хвосте (коллапс eligible сильно штрафуется).
- `ParentChangeAvg`, `FlappingAvg`: стабильность маршрутизационного дерева.

**Вывод:** метрики — это практические индикаторы качества сходимости и устойчивости дерева.

## 2) Методика исследования

- Топологий: 40
- Оценок (seed-run): 48800
- Всего запусков: 48800
- SeedCount: 10
- Итераций оптимизации на топологию: 40
- Раундов на одну оценку: 300

Топологии (nodeCount x linkRadius):

- `10x60`
- `10x90`
- `10x120`
- `10x160`
- `14x70`
- `14x100`
- `14x140`
- `14x180`
- `18x80`
- `18x120`
- `18x160`
- `18x210`
- `24x100`
- `24x140`
- `24x190`
- `24x240`
- `34x120`
- `34x170`
- `34x220`
- `34x280`
- `48x150`
- `48x210`
- `48x270`
- `48x330`
- `64x170`
- `64x230`
- `64x290`
- `64x350`
- `80x200`
- `80x260`
- `80x320`
- `80x380`
- `96x220`
- `96x280`
- `96x340`
- `96x400`
- `120x250`
- `120x320`
- `120x390`
- `120x460`

### 2.1 Как читать корреляции/влияние (без формул)

В отчете есть корреляции (Pearson/Spearman) и агрегат `ImpactIndex`. Они нужны **только** чтобы понять, какие ручки чаще всего связаны с качеством в этой выборке.

- Это **не доказательство причинности** и не ‘доказательство теоремы’.
- `ImpactIndex` = среднее $|r|$ по ключевым исходам и используется для ранжирования/отсечения почти нейтральных параметров.

**Вывод:** корреляции здесь — инструмент для упрощения настройки, а математическое обоснование протокола лежит в `_docs_v1.0/math/theorem.md`.

## 3) Результаты

### 3.1 Сводка по каждой топологии

| NetworkId | N | R | AvgScore | StableRatio | Verdict |
| --- | --- | --- | --- | --- | --- |
| 10x60 | 10 | 60 | 84.00 | 100.00% | STABLE |
| 10x90 | 10 | 90 | 84.00 | 100.00% | STABLE |
| 10x120 | 10 | 120 | 83.84 | 100.00% | STABLE |
| 10x160 | 10 | 160 | 84.21 | 100.00% | STABLE |
| 14x70 | 14 | 70 | 84.09 | 100.00% | STABLE |
| 14x100 | 14 | 100 | 84.09 | 100.00% | STABLE |
| 14x140 | 14 | 140 | 85.25 | 100.00% | STABLE |
| 14x180 | 14 | 180 | 91.35 | 90.00% | STABLE |
| 18x80 | 18 | 80 | 84.60 | 100.00% | STABLE |
| 18x120 | 18 | 120 | 77.50 | 60.00% | OSCILLATING |
| 18x160 | 18 | 160 | 98.61 | 100.00% | STABLE |
| 18x210 | 18 | 210 | 100.00 | 100.00% | STABLE |
| 24x100 | 24 | 100 | 96.95 | 100.00% | STABLE |
| 24x140 | 24 | 140 | 99.84 | 100.00% | STABLE |
| 24x190 | 24 | 190 | 100.00 | 100.00% | STABLE |
| 24x240 | 24 | 240 | 100.00 | 100.00% | STABLE |
| 34x120 | 34 | 120 | 100.00 | 100.00% | STABLE |
| 34x170 | 34 | 170 | 100.00 | 100.00% | STABLE |
| 34x220 | 34 | 220 | 100.00 | 100.00% | STABLE |
| 34x280 | 34 | 280 | 100.00 | 100.00% | STABLE |
| 48x150 | 48 | 150 | 100.00 | 100.00% | STABLE |
| 48x210 | 48 | 210 | 100.00 | 100.00% | STABLE |
| 48x270 | 48 | 270 | 100.00 | 100.00% | STABLE |
| 48x330 | 48 | 330 | 100.00 | 100.00% | STABLE |
| 64x170 | 64 | 170 | 92.96 | 90.00% | STABLE |
| 64x230 | 64 | 230 | 100.00 | 100.00% | STABLE |
| 64x290 | 64 | 290 | 100.00 | 100.00% | STABLE |
| 64x350 | 64 | 350 | 100.00 | 100.00% | STABLE |
| 80x200 | 80 | 200 | 100.00 | 100.00% | STABLE |
| 80x260 | 80 | 260 | 100.00 | 100.00% | STABLE |
| 80x320 | 80 | 320 | 100.00 | 100.00% | STABLE |
| 80x380 | 80 | 380 | 100.00 | 100.00% | STABLE |
| 96x220 | 96 | 220 | 100.00 | 100.00% | STABLE |
| 96x280 | 96 | 280 | 100.00 | 100.00% | STABLE |
| 96x340 | 96 | 340 | 100.00 | 100.00% | STABLE |
| 96x400 | 96 | 400 | 100.00 | 100.00% | STABLE |
| 120x250 | 120 | 250 | 100.00 | 100.00% | STABLE |
| 120x320 | 120 | 320 | 100.00 | 100.00% | STABLE |
| 120x390 | 120 | 390 | 100.00 | 100.00% | STABLE |
| 120x460 | 120 | 460 | 100.00 | 100.00% | STABLE |

**Вывод:** рост N и R в этой выборке в среднем связан с ростом Score и улучшением динамики дублей.

### 3.2 Лучший общий случай

- Сеть: **N=18, R=210**
- Score: **100.00**
- StableRatio: **100.00%**
- Verdict: **STABLE**

Лучшие параметры (для этой топологии):

```json
{
  "qForward": 173.0,
  "deliveryProbability": 0.15,
  "rootSourceCharge": 2200.0,
  "penaltyLambda": 49.0,
  "switchHysteresis": 57.0,
  "switchHysteresisRatio": 0.07,
  "chargeDropPerHop": 206.0,
  "chargeSpreadFactor": 0.05,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.22,
  "linkMemory": 0.867,
  "linkLearningRate": 0.25,
  "linkBonusMax": 14.0
}
```

**Вывод:** лучший режим в этой выборке (здесь N=18, R=210) дает полностью стабильное поведение в рамках выбранной эвристики Score.

### 3.3 Универсальный baseline параметров (1 профиль)

Это **один** профиль параметров, который можно брать как стартовый для разных сетей. Он получен как median по топ-20% запусков по Score (по всей выборке).

```json
{
  "qForward": 194.0,
  "deliveryProbability": 0.61,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 28.0,
  "switchHysteresis": 9.0,
  "switchHysteresisRatio": 0.07,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.28,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.13,
  "linkMemory": 0.912,
  "linkLearningRate": 0.23,
  "linkBonusMax": 60.0
}
```

Если нужно оставить минимум ручек: тюните `decayIntervalSteps`, `chargeSpreadFactor`, `switchHysteresisRatio`, а остальные параметры фиксируйте на baseline.

**Вывод:** baseline хорошо описывает типичный успешный режим; если нужна максимальная надежность на sparse-сетях, смотрите отклонения по классам ниже и хвост худших сценариев.

### 3.4 Лучшие параметры по классам топологий

| Класс | Топологий | MeanScore | MeanStable | Лучшая сеть | Вердикты |
| --- | --- | --- | --- | --- | --- |
| small-sparse | 8 | 83.42 | 95.00% | 14x140 | S:7/O:1/U:0 |
| small-medium | 4 | 93.54 | 97.50% | 18x210 | S:4/O:0/U:0 |
| medium-sparse | 3 | 98.93 | 100.00% | 34x120 | S:3/O:0/U:0 |
| medium-medium | 6 | 100.00 | 100.00% | 24x190 | S:6/O:0/U:0 |
| medium-dense | 3 | 100.00 | 100.00% | 34x280 | S:3/O:0/U:0 |
| large-medium | 4 | 98.24 | 97.50% | 64x230 | S:4/O:0/U:0 |
| large-dense | 4 | 100.00 | 100.00% | 64x290 | S:4/O:0/U:0 |
| xlarge-medium | 2 | 100.00 | 100.00% | 96x220 | S:2/O:0/U:0 |
| xlarge-dense | 6 | 100.00 | 100.00% | 96x280 | S:6/O:0/U:0 |

#### Класс `small-sparse`

- Топологий в классе: **8**
- MeanScore: **83.42** (σ=2.28)
- MeanStableRatio: **95.00%**
- Лучшая сеть: **14x140** (N=14, R=140)

Лучшие параметры (полный набор):

```json
{
  "qForward": 672.0,
  "deliveryProbability": 0.72,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 17.0,
  "switchHysteresis": 5.0,
  "switchHysteresisRatio": 0.04,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.27,
  "decayIntervalSteps": 30.0,
  "decayPercent": 0.23,
  "linkMemory": 0.94,
  "linkLearningRate": 0.06,
  "linkBonusMax": 38.0
}
```

Ключевые отличия от глобального baseline:

- `qForward`: 672.0 vs 194.0 (Δ=+478.000, ~246% от baseline)
- `decayIntervalSteps`: 30.0 vs 60.0 (Δ=-30.000, ~50% от baseline)
- `switchHysteresis`: 5.0 vs 9.0 (Δ=-4.000, ~44% от baseline)
- `penaltyLambda`: 17.0 vs 28.0 (Δ=-11.000, ~39% от baseline)
- `linkBonusMax`: 38.0 vs 60.0 (Δ=-22.000, ~37% от baseline)
- `linkLearningRate`: 0.06 vs 0.23 (Δ=-0.170, ~17% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `qForward` ↑, `decayIntervalSteps` ↓, `switchHysteresis` ↓.

#### Класс `small-medium`

- Топологий в классе: **4**
- MeanScore: **93.54** (σ=6.31)
- MeanStableRatio: **97.50%**
- Лучшая сеть: **18x210** (N=18, R=210)

Лучшие параметры (полный набор):

```json
{
  "qForward": 173.0,
  "deliveryProbability": 0.15,
  "rootSourceCharge": 2200.0,
  "penaltyLambda": 49.0,
  "switchHysteresis": 57.0,
  "switchHysteresisRatio": 0.07,
  "chargeDropPerHop": 206.0,
  "chargeSpreadFactor": 0.05,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.22,
  "linkMemory": 0.867,
  "linkLearningRate": 0.25,
  "linkBonusMax": 14.0
}
```

Ключевые отличия от глобального baseline:

- `switchHysteresis`: 57.0 vs 9.0 (Δ=+48.000, ~533% от baseline)
- `chargeDropPerHop`: 206.0 vs 80.0 (Δ=+126.000, ~158% от baseline)
- `linkBonusMax`: 14.0 vs 60.0 (Δ=-46.000, ~77% от baseline)
- `penaltyLambda`: 49.0 vs 28.0 (Δ=+21.000, ~75% от baseline)
- `rootSourceCharge`: 2200.0 vs 1500.0 (Δ=+700.000, ~47% от baseline)
- `deliveryProbability`: 0.15 vs 0.61 (Δ=-0.460, ~46% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `switchHysteresis` ↑, `chargeDropPerHop` ↑, `linkBonusMax` ↓.

#### Класс `medium-sparse`

- Топологий в классе: **3**
- MeanScore: **98.93** (σ=1.40)
- MeanStableRatio: **100.00%**
- Лучшая сеть: **34x120** (N=34, R=120)

Лучшие параметры (полный набор):

```json
{
  "qForward": 156.0,
  "deliveryProbability": 0.18,
  "rootSourceCharge": 1779.0,
  "penaltyLambda": 28.0,
  "switchHysteresis": 9.0,
  "switchHysteresisRatio": 0.14,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.05,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.12,
  "linkMemory": 0.968,
  "linkLearningRate": 0.69,
  "linkBonusMax": 77.0
}
```

Ключевые отличия от глобального baseline:

- `linkLearningRate`: 0.69 vs 0.23 (Δ=+0.460, ~46% от baseline)
- `deliveryProbability`: 0.18 vs 0.61 (Δ=-0.430, ~43% от baseline)
- `linkBonusMax`: 77.0 vs 60.0 (Δ=+17.000, ~28% от baseline)
- `chargeSpreadFactor`: 0.05 vs 0.28 (Δ=-0.230, ~23% от baseline)
- `qForward`: 156.0 vs 194.0 (Δ=-38.000, ~20% от baseline)
- `rootSourceCharge`: 1779.0 vs 1500.0 (Δ=+279.000, ~19% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `linkLearningRate` ↑, `deliveryProbability` ↓, `linkBonusMax` ↑.

#### Класс `medium-medium`

- Топологий в классе: **6**
- MeanScore: **100.00** (σ=0.00)
- MeanStableRatio: **100.00%**
- Лучшая сеть: **24x190** (N=24, R=190)

Лучшие параметры (полный набор):

```json
{
  "qForward": 72.0,
  "deliveryProbability": 0.15,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 28.0,
  "switchHysteresis": 0.0,
  "switchHysteresisRatio": 0.12,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.05,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.12,
  "linkMemory": 0.912,
  "linkLearningRate": 0.71,
  "linkBonusMax": 84.0
}
```

Ключевые отличия от глобального baseline:

- `switchHysteresis`: 0.0 vs 9.0 (Δ=-9.000, ~100% от baseline)
- `qForward`: 72.0 vs 194.0 (Δ=-122.000, ~63% от baseline)
- `linkLearningRate`: 0.71 vs 0.23 (Δ=+0.480, ~48% от baseline)
- `deliveryProbability`: 0.15 vs 0.61 (Δ=-0.460, ~46% от baseline)
- `linkBonusMax`: 84.0 vs 60.0 (Δ=+24.000, ~40% от baseline)
- `chargeSpreadFactor`: 0.05 vs 0.28 (Δ=-0.230, ~23% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `switchHysteresis` ↓, `qForward` ↓, `linkLearningRate` ↑.

#### Класс `medium-dense`

- Топологий в классе: **3**
- MeanScore: **100.00** (σ=0.00)
- MeanStableRatio: **100.00%**
- Лучшая сеть: **34x280** (N=34, R=280)

Лучшие параметры (полный набор):

```json
{
  "qForward": 220.0,
  "deliveryProbability": 0.72,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 28.0,
  "switchHysteresis": 9.0,
  "switchHysteresisRatio": 0.03,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.28,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.12,
  "linkMemory": 0.94,
  "linkLearningRate": 0.2,
  "linkBonusMax": 45.0
}
```

Ключевые отличия от глобального baseline:

- `linkBonusMax`: 45.0 vs 60.0 (Δ=-15.000, ~25% от baseline)
- `qForward`: 220.0 vs 194.0 (Δ=+26.000, ~13% от baseline)
- `deliveryProbability`: 0.72 vs 0.61 (Δ=+0.110, ~11% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `linkBonusMax` ↓, `qForward` ↑, `deliveryProbability` ↑.

#### Класс `large-medium`

- Топологий в классе: **4**
- MeanScore: **98.24** (σ=3.05)
- MeanStableRatio: **97.50%**
- Лучшая сеть: **64x230** (N=64, R=230)

Лучшие параметры (полный набор):

```json
{
  "qForward": 20.0,
  "deliveryProbability": 0.39,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 0.0,
  "switchHysteresis": 0.0,
  "switchHysteresisRatio": 0.03,
  "chargeDropPerHop": 10.0,
  "chargeSpreadFactor": 0.46,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.2,
  "linkMemory": 0.94,
  "linkLearningRate": 0.2,
  "linkBonusMax": 97.0
}
```

Ключевые отличия от глобального baseline:

- `penaltyLambda`: 0.0 vs 28.0 (Δ=-28.000, ~100% от baseline)
- `switchHysteresis`: 0.0 vs 9.0 (Δ=-9.000, ~100% от baseline)
- `qForward`: 20.0 vs 194.0 (Δ=-174.000, ~90% от baseline)
- `chargeDropPerHop`: 10.0 vs 80.0 (Δ=-70.000, ~88% от baseline)
- `linkBonusMax`: 97.0 vs 60.0 (Δ=+37.000, ~62% от baseline)
- `deliveryProbability`: 0.39 vs 0.61 (Δ=-0.220, ~22% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `penaltyLambda` ↓, `switchHysteresis` ↓, `qForward` ↓.

#### Класс `large-dense`

- Топологий в классе: **4**
- MeanScore: **100.00** (σ=0.00)
- MeanStableRatio: **100.00%**
- Лучшая сеть: **64x290** (N=64, R=290)

Лучшие параметры (полный набор):

```json
{
  "qForward": 20.0,
  "deliveryProbability": 0.72,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 28.0,
  "switchHysteresis": 9.0,
  "switchHysteresisRatio": 0.03,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.7,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.12,
  "linkMemory": 0.995,
  "linkLearningRate": 0.2,
  "linkBonusMax": 45.0
}
```

Ключевые отличия от глобального baseline:

- `qForward`: 20.0 vs 194.0 (Δ=-174.000, ~90% от baseline)
- `chargeSpreadFactor`: 0.7 vs 0.28 (Δ=+0.420, ~42% от baseline)
- `linkBonusMax`: 45.0 vs 60.0 (Δ=-15.000, ~25% от baseline)
- `deliveryProbability`: 0.72 vs 0.61 (Δ=+0.110, ~11% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `qForward` ↓, `chargeSpreadFactor` ↑, `linkBonusMax` ↓.

#### Класс `xlarge-medium`

- Топологий в классе: **2**
- MeanScore: **100.00** (σ=0.00)
- MeanStableRatio: **100.00%**
- Лучшая сеть: **96x220** (N=96, R=220)

Лучшие параметры (полный набор):

```json
{
  "qForward": 220.0,
  "deliveryProbability": 0.72,
  "rootSourceCharge": 1500.0,
  "penaltyLambda": 0.0,
  "switchHysteresis": 108.0,
  "switchHysteresisRatio": 0.03,
  "chargeDropPerHop": 80.0,
  "chargeSpreadFactor": 0.28,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.12,
  "linkMemory": 0.94,
  "linkLearningRate": 0.2,
  "linkBonusMax": 120.0
}
```

Ключевые отличия от глобального baseline:

- `switchHysteresis`: 108.0 vs 9.0 (Δ=+99.000, ~1100% от baseline)
- `penaltyLambda`: 0.0 vs 28.0 (Δ=-28.000, ~100% от baseline)
- `linkBonusMax`: 120.0 vs 60.0 (Δ=+60.000, ~100% от baseline)
- `qForward`: 220.0 vs 194.0 (Δ=+26.000, ~13% от baseline)
- `deliveryProbability`: 0.72 vs 0.61 (Δ=+0.110, ~11% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `switchHysteresis` ↑, `penaltyLambda` ↓, `linkBonusMax` ↑. (в классе мало топологий, вывод менее надежен)

#### Класс `xlarge-dense`

- Топологий в классе: **6**
- MeanScore: **100.00** (σ=0.00)
- MeanStableRatio: **100.00%**
- Лучшая сеть: **96x280** (N=96, R=280)

Лучшие параметры (полный набор):

```json
{
  "qForward": 20.0,
  "deliveryProbability": 0.72,
  "rootSourceCharge": 1903.0,
  "penaltyLambda": 0.0,
  "switchHysteresis": 9.0,
  "switchHysteresisRatio": 0.03,
  "chargeDropPerHop": 21.0,
  "chargeSpreadFactor": 0.28,
  "decayIntervalSteps": 60.0,
  "decayPercent": 0.0,
  "linkMemory": 0.94,
  "linkLearningRate": 0.2,
  "linkBonusMax": 98.0
}
```

Ключевые отличия от глобального baseline:

- `penaltyLambda`: 0.0 vs 28.0 (Δ=-28.000, ~100% от baseline)
- `qForward`: 20.0 vs 194.0 (Δ=-174.000, ~90% от baseline)
- `chargeDropPerHop`: 21.0 vs 80.0 (Δ=-59.000, ~74% от baseline)
- `linkBonusMax`: 98.0 vs 60.0 (Δ=+38.000, ~63% от baseline)
- `rootSourceCharge`: 1903.0 vs 1500.0 (Δ=+403.000, ~27% от baseline)
- `decayPercent`: 0.0 vs 0.13 (Δ=-0.130, ~13% от baseline)

**Вывод (по классу):** оптимизатор ушел от baseline: `penaltyLambda` ↓, `qForward` ↓, `chargeDropPerHop` ↓.

### 3.5 Влияние параметров (и вывод по каждому параметру)

`ImpactIndex` = среднее значение $|r|$ по набору исходов (Score, pass-rate, покрытие, стабильность). Чем выше — тем сильнее параметр ‘двигает’ поведение в этой выборке.

Топ влияния:

| Параметр | ImpactIndex | Сильнейшая метрика | r | Интерпретация |
| --- | --- | --- | --- | --- |
| decayIntervalSteps | 0.118 | theoremPassRate | -0.210 | выше обычно ухудшает сильнейшую метрику |
| chargeSpreadFactor | 0.095 | duplicateDrop | 0.218 | выше обычно улучшает сильнейшую метрику |
| switchHysteresisRatio | 0.090 | score | 0.237 | выше обычно улучшает сильнейшую метрику |
| rootSourceCharge | 0.069 | duplicateDrop | -0.148 | выше обычно ухудшает сильнейшую метрику |
| chargeDropPerHop | 0.067 | theoremPassRate | -0.147 | выше обычно ухудшает сильнейшую метрику |
| deliveryProbability | 0.058 | theoremPassRate | -0.089 | выше обычно ухудшает сильнейшую метрику |

Низ влияния (почти не меняют картину в этой выборке):

| Параметр | ImpactIndex | Сильнейшая метрика | r |
| --- | --- | --- | --- |
| decayPercent | 0.051 | duplicateDrop | -0.113 |
| penaltyLambda | 0.048 | duplicateDrop | -0.192 |
| linkMemory | 0.046 | theoremPassRate | 0.079 |
| switchHysteresis | 0.042 | eligibleTailRatio | -0.114 |
| linkLearningRate | 0.034 | score | 0.141 |
| linkBonusMax | 0.020 | duplicateDrop | -0.070 |

Выводы по каждому параметру (коротко):

| Параметр | Смысл | ImpactIndex | r(Score) | Сильнее всего связано | Вывод |
| --- | --- | --- | --- | --- | --- |
| qForward | Порог eligible: узел eligible если q_total ≥ qForward. | 0.052 | -0.018 | eligibleTailRatio (-0.197) | Score: связь слабая; увеличение чаще ухудшает метрику |
| deliveryProbability | Базовая вероятность доставки (далее масштабируется качеством линка). | 0.058 | -0.079 | theoremPassRate (-0.089) | Score: связь слабая; увеличение чаще ухудшает метрику |
| rootSourceCharge | Инъекция заряда в gateway каждый раунд (источник энергии). | 0.069 | +0.057 | duplicateDrop (-0.148) | Score: связь слабая; увеличение чаще ухудшает метрику |
| penaltyLambda | Штраф слабых линков при выборе родителя (выше = сильнее избегаем плохие связи). | 0.048 | -0.053 | duplicateDrop (-0.192) | Score: связь слабая; увеличение чаще ухудшает метрику |
| switchHysteresis | Абсолютный гистерезис смены родителя (выше = меньше переключений). | 0.042 | -0.086 | eligibleTailRatio (-0.114) | Score: связь слабая; увеличение чаще ухудшает метрику |
| switchHysteresisRatio | Относительный гистерезис (доля от оценки), добавляется к switchHysteresis. | 0.090 | +0.237 | score (+0.237) | скорее ↑Score; увеличение чаще улучшает метрику |
| chargeDropPerHop | Потеря заряда на хоп (ограничивает глубину распространения). | 0.067 | -0.051 | theoremPassRate (-0.147) | Score: связь слабая; увеличение чаще ухудшает метрику |
| chargeSpreadFactor | Скорость смешивания/распространения заряда к целевому уровню. | 0.095 | +0.082 | duplicateDrop (+0.218) | Score: связь слабая; увеличение чаще улучшает метрику |
| decayIntervalSteps | Интервал глобального decay (0 = нет decay; больше = реже). | 0.118 | -0.142 | theoremPassRate (-0.210) | скорее ↓Score; увеличение чаще ухудшает метрику |
| decayPercent | Сила decay в эпоху (доля, которая вычитается). | 0.051 | -0.105 | duplicateDrop (-0.113) | скорее ↓Score; увеличение чаще ухудшает метрику |
| linkMemory | Память накопленного usageScore (ближе к 1 = более инерционно). | 0.046 | -0.010 | theoremPassRate (+0.079) | Score: связь слабая; увеличение чаще улучшает метрику |
| linkLearningRate | Скорость усиления effectiveQuality от usageScore. | 0.034 | +0.141 | score (+0.141) | скорее ↑Score; увеличение чаще улучшает метрику |
| linkBonusMax | Макс. бонус стабильности линка в выборе родителя. | 0.020 | -0.014 | duplicateDrop (-0.070) | Score: связь слабая; увеличение чаще ухудшает метрику |

**Общий вывод по параметрам:** в этой выборке заметнее всего влияют `decayIntervalSteps`, `chargeSpreadFactor`, `switchHysteresisRatio`. Корреляции — описательные, и часть эффектов может быть связана с совместной настройкой параметров оптимизатором.

### 3.6 Связки параметр↔параметр (важно для интерпретации)

| Пара | Pearson | |Pearson| |
| --- | --- | --- |
| deliveryProbability <> linkLearningRate | -0.447 | 0.447 |
| penaltyLambda <> switchHysteresis | 0.425 | 0.425 |
| switchHysteresisRatio <> linkLearningRate | 0.386 | 0.386 |
| chargeDropPerHop <> decayIntervalSteps | 0.333 | 0.333 |
| decayPercent <> linkMemory | -0.313 | 0.313 |
| qForward <> switchHysteresis | 0.303 | 0.303 |
| deliveryProbability <> penaltyLambda | -0.297 | 0.297 |
| penaltyLambda <> chargeSpreadFactor | -0.287 | 0.287 |
| chargeDropPerHop <> linkMemory | -0.281 | 0.281 |
| decayIntervalSteps <> linkMemory | -0.273 | 0.273 |
| deliveryProbability <> switchHysteresis | -0.261 | 0.261 |
| deliveryProbability <> chargeSpreadFactor | 0.240 | 0.240 |
| switchHysteresis <> switchHysteresisRatio | -0.228 | 0.228 |
| rootSourceCharge <> decayIntervalSteps | -0.223 | 0.223 |
| qForward <> penaltyLambda | 0.220 | 0.220 |
| switchHysteresis <> decayIntervalSteps | 0.216 | 0.216 |
| switchHysteresisRatio <> chargeDropPerHop | 0.214 | 0.214 |
| linkMemory <> linkLearningRate | -0.196 | 0.196 |
| chargeSpreadFactor <> decayIntervalSteps | -0.194 | 0.194 |
| rootSourceCharge <> penaltyLambda | 0.189 | 0.189 |

**Вывод:** некоторые параметры сильно коррелируют между собой (например, `deliveryProbability` ↔ `penaltyLambda`). Это значит, что одиночная корреляция параметра с Score может отражать совместную настройку, а не ‘чистый’ эффект.

### 3.7 Эффект топологии

| Исход | r(nodeCount) | r(linkRadius) |
| --- | --- | --- |
| Score (итоговая оценка) | 0.438 | 0.496 |
| TheoremPassRate (доля раундов, где теорема верна) | 0.147 | 0.175 |
| AssumptionsPassRate (доля раундов, где аксиомы верны) | 0.147 | 0.175 |
| CoverageAvg (среднее покрытие DOWN) | 0.074 | 0.124 |
| DuplicateDrop (снижение дублей DOWN) | 0.800 | 0.626 |
| EligibleTailRatio (здоровье eligible-набора в конце) | 0.030 | 0.048 |
| ParentChangeAvg (среднее число смен родителя) | 0.070 | 0.037 |
| FlappingAvg (флаппинг родителей) | 0.062 | 0.039 |

**Вывод:** в этой выборке более крупные и более плотные топологии в среднем показывают более высокий Score и более выраженное снижение дублей (DuplicateDrop).

### 3.8 Нестабильные и худшие случаи

| Вердикт | Count | Share |
| --- | --- | --- |
| STABLE | 45989 | 94.24% |
| OSCILLATING | 1357 | 2.78% |
| UNSTABLE | 1454 | 2.98% |

Худшие seed-run (минимальный Score):

| RunId | Network | Seed | Verdict | Score | TheoremPass | Coverage | DupDrop | ParentChange | Flapping |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 48x150@48 | 48x150 | 48 | UNSTABLE | 17.16 | 7.67% | 27.17% | -0.09 | 1.74 | 2.77 |
| 64x170@49 | 64x170 | 49 | UNSTABLE | 20.24 | 5.67% | 39.65% | 14.89 | 5.54 | 9.25 |
| 96x400@44 | 96x400 | 44 | UNSTABLE | 21.92 | 16.33% | 99.93% | 3.19 | 14.58 | 26.80 |
| 96x400@47 | 96x400 | 47 | UNSTABLE | 22.42 | 10.33% | 99.84% | 20.91 | 17.93 | 33.38 |
| 96x400@48 | 96x400 | 48 | UNSTABLE | 22.43 | 1.67% | 99.87% | 45.68 | 15.02 | 28.24 |
| 96x220@49 | 96x220 | 49 | UNSTABLE | 23.65 | 12.00% | 100.00% | 7.36 | 9.57 | 17.14 |
| 96x340@48 | 96x340 | 48 | UNSTABLE | 25.46 | 17.33% | 100.00% | 13.43 | 11.69 | 18.31 |
| 96x400@50 | 96x400 | 50 | UNSTABLE | 25.54 | 23.00% | 99.99% | 7.57 | 13.59 | 24.31 |
| 80x260@49 | 80x260 | 49 | UNSTABLE | 26.23 | 11.00% | 100.00% | 7.79 | 6.32 | 10.78 |
| 96x400@49 | 96x400 | 49 | UNSTABLE | 26.61 | 26.33% | 98.76% | 6.20 | 12.16 | 21.49 |
| 120x390@48 | 120x390 | 48 | UNSTABLE | 26.71 | 7.00% | 100.00% | 114.85 | 22.53 | 36.31 |
| 18x120@50 | 18x120 | 50 | UNSTABLE | 26.91 | 3.33% | 38.85% | 0.00 | 0.05 | 0.00 |

Риск по топологиям (где чаще нестабильность и насколько плохой хвост):

Примечание: `UnstableShare` и `WorstUnstableScore` считаются по **всем** оценкам во время оптимизации (по всем переборам параметров), а не по лучшему найденному профилю для этой топологии (см. таблицу 3.1).

| Network | UnstableShare | WorstScore | WorstUnstableScore | WorstUnstableRunId |
| --- | --- | --- | --- | --- |
| 18x120 | 44.92% | 26.91 | 26.91 | 18x120@50 |
| 64x170 | 24.43% | 20.24 | 20.24 | 64x170@49 |
| 14x180 | 17.05% | 36.87 | 36.87 | 14x180@46 |
| 24x100 | 16.64% | 31.96 | 31.96 | 24x100@45 |
| 48x150 | 16.39% | 17.16 | 17.16 | 48x150@48 |
| 14x140 | 9.92% | 37.39 | 37.39 | 14x140@51 |
| 18x160 | 9.43% | 30.93 | 30.93 | 18x160@51 |
| 18x210 | 7.87% | 40.39 | 40.39 | 18x210@47 |
| 34x220 | 6.15% | 40.36 | 40.36 | 34x220@50 |
| 34x120 | 5.90% | 34.03 | 34.03 | 34x120@49 |
| 10x160 | 5.57% | 38.78 | 38.78 | 10x160@45 |
| 14x100 | 5.49% | 37.16 | 37.16 | 14x100@50 |
| 24x240 | 5.00% | 40.03 | 40.03 | 24x240@50 |
| 34x170 | 4.92% | 37.32 | 37.32 | 34x170@48 |
| 18x80 | 4.84% | 37.76 | 37.76 | 18x80@49 |

**Вывод:** нестабильность концентрируется в части топологий/seed-комбинаций; для инженерного выбора параметров важно смотреть не только на средний Score, но и на хвост худших сценариев.

## 4) Графики (с выводами)

### 4.1 Сводка прогона

### Score по топологиям

![Score по топологиям](charts/score_by_network.svg)

**Вывод:** видно распределение качества по сеткам; лучше сравнивать с классами (size/density).

### StableRatio по топологиям

![StableRatio по топологиям](charts/stable_ratio_by_network.svg)

**Вывод:** показывает, где режим стабилен (высокая доля STABLE) и где возможны колебания.

### Распределение вердиктов

![Распределение вердиктов](charts/verdict_distribution.svg)

**Вывод:** характеризует общий уровень устойчивости по выбранной метрике Score.

### Первый раунд, где проходят все проверки

![Первый раунд, где проходят все проверки](charts/first_all_checks_round_by_network.svg)

**Вывод:** чем меньше — тем быстрее сеть выходит в корректный режим (если он достигается).

### Раунд устойчивого прохождения всех проверок

![Раунд устойчивого прохождения всех проверок](charts/sustained_all_checks_round_by_network.svg)

**Вывод:** важнее, чем первый успех: показывает, когда режим перестает ‘проваливаться’.

### 4.2 Лучший случай (динамика)

### Топология лучшей сети

![Топология лучшей сети](charts/best_network_topology.svg)

**Вывод:** это пример топологии, где найден режим Score≈100 и высокий StableRatio.

### Покрытие и eligible

![Покрытие и eligible](charts/best_network_connectivity_dynamics.svg)

**Вывод:** важно следить, чтобы eligible-набор не коллапсировал в хвосте.

### Стабильность дерева

![Стабильность дерева](charts/best_network_stability.svg)

**Вывод:** меньше смен родителей и флаппинга = более устойчивый режим.

### Трафик и дубли

![Трафик и дубли](charts/best_network_routing_dynamics.svg)

**Вывод:** снижение дублей при сохранении покрытия — хороший признак сходимости.

### Прохождение теоремы/аксиом по раундам

![Прохождение теоремы/аксиом по раундам](charts/best_network_theorem_status.svg)

**Вывод:** показывает, насколько режим устойчиво проходит проверки.

### A5/A6/A7 и леммы по раундам

![A5/A6/A7 и леммы по раундам](charts/best_network_checks_status.svg)

**Вывод:** помогает понять, какая именно часть теоремы ломается, если ломается.

### 4.3 Худший нестабильный сценарий

### Число unstable/oscillating запусков по топологиям

![Число unstable/oscillating запусков по топологиям](charts/unstable_or_oscillating_runs_by_network.svg)

**Вывод:** показывает, в каких сетках чаще возникает деградация и нестабильность.

### Топология худшей сети

![Топология худшей сети](charts/worst_network_topology.svg)

**Вывод:** визуализация худшего случая с аннотацией зарядов узлов и средних весов ребер.

### Худший случай: покрытие и eligible

![Худший случай: покрытие и eligible](charts/worst_network_connectivity_dynamics.svg)

**Вывод:** обычно видно коллапс eligible-набора или слабое восстановление покрытия.

### Худший случай: стабильность дерева

![Худший случай: стабильность дерева](charts/worst_network_stability.svg)

**Вывод:** скачки parentChange/flapping указывают на маршрутизационную турбулентность.

### Худший случай: трафик и дубли

![Худший случай: трафик и дубли](charts/worst_network_routing_dynamics.svg)

**Вывод:** рост дублей при слабом покрытии — типичный признак неустойчивого режима.

### Худший случай: теорема/аксиомы по раундам

![Худший случай: теорема/аксиомы по раундам](charts/worst_network_theorem_status.svg)

**Вывод:** позволяет увидеть, это постоянный провал или редкие проблески прохождения проверок.

### Худший случай: A5/A6/A7 и леммы

![Худший случай: A5/A6/A7 и леммы](charts/worst_network_checks_status.svg)

**Вывод:** диагностирует, какая именно проверка ломается чаще всего.

### 4.4 Чувствительность и корреляции

### Индекс влияния параметров

![Индекс влияния параметров](charts/comprehensive/parameter_impact_index.svg)

**Вывод:** влияние распределено неравномерно: есть несколько параметров-лидеров и несколько почти нейтральных.

### Корреляции параметров со Score

![Корреляции параметров со Score](charts/comprehensive/score_correlation_signed.svg)

**Вывод:** видна направленность корреляций со Score, но интерпретировать нужно с учетом связок параметров.

### Тепловая карта корреляций (параметры ↔ исходы)

![Тепловая карта корреляций (параметры ↔ исходы)](charts/comprehensive/parameter_outcome_heatmap.svg)

**Вывод:** удобно видеть, на какие исходы сильнее влияет каждый параметр.

### Средний Score по классам

![Средний Score по классам](charts/comprehensive/class_mean_score.svg)

**Вывод:** плотность/размер класса напрямую отражаются в среднем Score (в этой выборке плотнее/крупнее лучше).

### Корреляции топологии с исходами

![Корреляции топологии с исходами](charts/comprehensive/topology_outcome_correlations.svg)

**Вывод:** в этой выборке `nodeCount` и `linkRadius` положительно связаны со Score и DuplicateDrop.

### Доли вердиктов в полной выборке

![Доли вердиктов в полной выборке](charts/comprehensive/verdict_share_comprehensive.svg)

**Вывод:** доли STABLE/OSCILLATING/UNSTABLE по всей расширенной выборке; это базовый индикатор общей устойчивости.

### Доля нестабильных запусков по классам

![Доля нестабильных запусков по классам](charts/comprehensive/unstable_ratio_by_class.svg)

**Вывод:** показывает, какие классы топологий дают более высокий риск неустойчивого режима.

### Худший Score по каждой топологии

![Худший Score по каждой топологии](charts/comprehensive/worst_score_by_network.svg)

**Вывод:** нижняя граница качества по каждой топологии; важно для оценки риска в production-сценариях.

### Score хвоста худших запусков

![Score хвоста худших запусков](charts/comprehensive/worst_runs_score_tail.svg)

**Вывод:** хвост самых плохих seed-run: чем длиннее и глубже хвост, тем выше риск редких провалов.

## Приложение A: Полная матрица Pearson (параметр ↔ исход)

| Parameter | Score | TheoremPass | AssumptionPass | Coverage | DuplicateDrop | EligibleTail | ParentChanges | Flapping |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| qForward | -0.018 | -0.043 | -0.043 | -0.035 | 0.049 | -0.197 | 0.025 | -0.002 |
| deliveryProbability | -0.079 | -0.089 | -0.089 | -0.064 | 0.068 | 0.068 | -0.007 | -0.003 |
| rootSourceCharge | 0.057 | 0.145 | 0.145 | 0.044 | -0.148 | 0.006 | -0.007 | -0.003 |
| penaltyLambda | -0.053 | 0.010 | 0.010 | 0.011 | -0.192 | -0.050 | -0.029 | -0.027 |
| switchHysteresis | -0.086 | -0.036 | -0.036 | 0.017 | 0.006 | -0.114 | -0.013 | -0.030 |
| switchHysteresisRatio | 0.237 | 0.070 | 0.070 | 0.018 | 0.187 | 0.054 | -0.047 | -0.038 |
| chargeDropPerHop | -0.051 | -0.147 | -0.147 | -0.092 | -0.018 | -0.036 | 0.025 | 0.020 |
| chargeSpreadFactor | 0.082 | 0.114 | 0.114 | 0.063 | 0.218 | -0.027 | 0.078 | 0.061 |
| decayIntervalSteps | -0.142 | -0.210 | -0.210 | -0.106 | -0.066 | -0.018 | 0.092 | 0.098 |
| decayPercent | -0.105 | -0.041 | -0.041 | -0.035 | -0.113 | -0.029 | -0.024 | -0.019 |
| linkMemory | -0.010 | 0.079 | 0.079 | 0.075 | -0.057 | 0.007 | -0.028 | -0.033 |
| linkLearningRate | 0.141 | -0.007 | -0.007 | -0.039 | -0.035 | -0.017 | -0.014 | -0.013 |
| linkBonusMax | -0.014 | 0.001 | 0.001 | 0.024 | -0.070 | 0.019 | -0.016 | -0.012 |

## Приложение B: Входной запрос

```json
{
  "baseConfig": {
    "nodeCount": 48,
    "linkRadius": 210,
    "seed": 42,
    "maxRounds": 320
  },
  "seedCount": 10,
  "optimizationIterations": 50,
  "roundsPerCheck": 300,
  "matrixText": "10x60,10x90,10x120,10x160,14x70,14x100,14x140,14x180,18x80,18x120,18x160,18x210,24x100,24x140,24x190,24x240,34x120,34x170,34x220,34x280,48x150,48x210,48x270,48x330,64x170,64x230,64x290,64x350,80x200,80x260,80x320,80x380,96x220,96x280,96x340,96x400,120x250,120x320,120x390,120x460",
  "parallelWorkers": 5
}
```

## Общий вывод

1) Этот прогон показывает, что протокол способен выходить в режим, где проверки теоремы (A5/A6/A7 + Леммы 4.1–4.3) выполняются устойчиво (см. графики first/sustained all-checks по топологиям).
2) Риск редких провалов (tail) в этой выборке: нестабильные/осциллирующие = **5.76% (48800 оценок)**.
3) Есть один практический baseline параметров (median топ-20% лучших запусков) — его достаточно, чтобы начать.
4) Чтобы ‘убрать шум’ и тюнить только важное, начните с: `decayIntervalSteps`, `chargeSpreadFactor`, `switchHysteresisRatio`. Остальные параметры фиксируйте на baseline.
5) Математическое обоснование структуры дерева и loop-free DOWN: см. `_docs_v1.0/math/theorem.md`. Симуляция проверяет именно эти предпосылки (A5/A6/A7 и Леммы), поэтому практическая часть напрямую связана с теоремой.
