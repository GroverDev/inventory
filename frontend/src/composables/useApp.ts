import { ref } from 'vue';

export function useApp() {

    // Fullscreen Logic
    const toggleFullscreen = () => {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch((e) => {
                console.error(`Error attempting to enable fullscreen mode: ${e.message} (${e.name})`);
            });
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen();
            }
        }
    };

    // Audio Logic (Simple implementation)
    const playSound = (soundFile: string, volume: number = 0.5) => {
        // Assume sounds are in public/media/sound/
        // Adjust path as per project structure
        const path = `/media/sound/${soundFile}`;
        const audio = new Audio(path);
        audio.volume = volume;
        audio.play().catch(e => console.error("Error playing sound:", e));
    };

    // Print
    const printPage = () => {
        window.print();
    };

    const initTooltips = () => {
        // @ts-ignore
        if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
            // @ts-ignore
            const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
            // @ts-ignore
            tooltipTriggerList.map(function (tooltipTriggerEl) {
                // @ts-ignore
                return new bootstrap.Tooltip(tooltipTriggerEl);
            });
        }
    };

    const initPopovers = () => {
        // @ts-ignore
        if (typeof bootstrap !== 'undefined' && bootstrap.Popover) {
            // @ts-ignore
            const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
            // @ts-ignore
            popoverTriggerList.map(function (popoverTriggerEl) {
                // @ts-ignore
                return new bootstrap.Popover(popoverTriggerEl);
            });
        }
    };

    return {
        toggleFullscreen,
        playSound,
        printPage,
        initTooltips,
        initPopovers,
    };
}
