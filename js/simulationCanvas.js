window.simulationCanvas = {
    getContainerSize: function (container) {
        if (!container) {
            return { width: 2000, height: 1500 };
        }

        const rect = container.getBoundingClientRect();
        return {
            width: rect.width,
            height: rect.height
        };
    },

    initResizeListener: function (dotNetRef) {
        const handleResize = () => {
            dotNetRef.invokeMethodAsync('OnWindowResized');
        };

        window.addEventListener('resize', handleResize);

        const observer = new ResizeObserver(handleResize);
        const container = document.querySelector('.simulation-canvas-container');
        if (container) {
            observer.observe(container);
        }

        return {
            dispose: () => {
                window.removeEventListener('resize', handleResize);
                observer.disconnect();
            }
        };
    }
};

window.fileUtils = {
    downloadFile: function (filename, content, mimeType) {
        const blob = new Blob([content], { type: mimeType });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },
    
    uploadFile: function (inputId) {
        return new Promise((resolve, reject) => {
            const input = document.getElementById(inputId);
            if (!input || !input.files || input.files.length === 0) {
                reject('No file selected');
                return;
            }
            
            const file = input.files[0];
            const reader = new FileReader();
            
            reader.onload = function(e) {
                resolve(e.target.result);
            };
            
            reader.onerror = function() {
                reject('Error reading file');
            };
            
            reader.readAsText(file);
        });
    },
    
    triggerFileInput: function(inputId) {
        const input = document.getElementById(inputId);
        if (input) {
            input.click();
        }
    }
};