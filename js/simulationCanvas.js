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