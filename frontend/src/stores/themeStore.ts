import { defineStore } from 'pinia';
import { ref, watch } from 'vue';

export const useThemeStore = defineStore('theme', () => {
    // State
    const theme = ref<string>(localStorage.getItem('theme') || 'light');
    const themeStyle = ref<string>(localStorage.getItem('themeStyle') || '');

    // Initialize
    const initTheme = () => {
        // Apply initial state
        updateThemeAttribute(theme.value);
        if (themeStyle.value) {
            updateThemeStyleLink(themeStyle.value);
        }
    };

    // Actions
    const setTheme = (newTheme: string) => {
        theme.value = newTheme;
        updateThemeAttribute(newTheme);
        localStorage.setItem('theme', newTheme);
    };

    const toggleTheme = () => {
        const newTheme = theme.value === 'light' ? 'dark' : 'light';
        setTheme(newTheme);
    };

    const setThemeStyle = (path: string) => {
        themeStyle.value = path;
        updateThemeStyleLink(path);
        localStorage.setItem('themeStyle', path);
    };

    // Helpers
    const updateThemeAttribute = (themeVal: string) => {
        const html = document.documentElement;
        html.setAttribute('data-bs-theme', themeVal);
    };

    const updateThemeStyleLink = (path: string) => {
        let linkElement = document.getElementById('theme-style') as HTMLLinkElement;

        if (!path) {
            if (linkElement) linkElement.href = '';
            return;
        }

        if (linkElement) {
            linkElement.href = path;
        } else {
            linkElement = document.createElement('link');
            linkElement.id = 'theme-style';
            linkElement.rel = 'stylesheet';
            linkElement.media = 'screen';
            linkElement.href = path;
            document.head.appendChild(linkElement);
        }
    };

    // Watchers for persistence (optional if using explicit actions, but good safety)
    watch(theme, (newVal) => {
        localStorage.setItem('theme', newVal);
        updateThemeAttribute(newVal);
    });

    watch(themeStyle, (newVal) => {
        localStorage.setItem('themeStyle', newVal);
        updateThemeStyleLink(newVal);
    });

    return {
        theme,
        themeStyle,
        initTheme,
        setTheme,
        toggleTheme,
        setThemeStyle
    };
});
