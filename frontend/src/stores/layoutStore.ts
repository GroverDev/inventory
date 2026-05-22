import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useLayoutStore = defineStore('layout', () => {
    // State
    const mobileMenuOpen = ref(false);
    const settingsDrawerOpen = ref(false);
    const appDrawerOpen = ref(false);
    const navMinified = ref(false);
    const filterText = ref('');

    // Actions
    const toggleMobileMenu = () => {
        mobileMenuOpen.value = !mobileMenuOpen.value;
        updateBodyClass('app-mobile-menu-open', mobileMenuOpen.value); // This one seems to be correct based on Sidebar component logic, but let's check smartApp.js memory if possible. Assuming this is fine for now as it wasn't in the set-* list.
    };

    const closeMobileMenu = () => {
        mobileMenuOpen.value = false;
        updateBodyClass('app-mobile-menu-open', false);
    };

    const toggleSettingsDrawer = () => {
        settingsDrawerOpen.value = !settingsDrawerOpen.value;
        // Logic to update DOM will be handled by binding class in the component or here if it's outside Vue root (unlikely)
        // But for now, let's assume we bind :class="{ open: layoutStore.settingsDrawerOpen }" on the component
    };

    const toggleAppDrawer = () => {
        appDrawerOpen.value = !appDrawerOpen.value;
    };

    const toggleNavMinified = () => {
        navMinified.value = !navMinified.value;
        updateBodyClass('set-nav-minified', navMinified.value);
    };

    const toggleHeaderFixed = () => {
        updateBodyClass('set-header-fixed', toggleState('headerFixed'));
    };

    const toggleNavFixed = () => {
        updateBodyClass('set-nav-fixed', toggleState('navFixed'));
    };

    const toggleNavFull = () => {
        updateBodyClass('set-nav-full', toggleState('navFull'));
    };

    const toggleNavCollapsed = () => {
        updateBodyClass('set-nav-collapsed', toggleState('navCollapsed'));
    };

    const toggleColorblindMode = () => {
        updateBodyClass('set-colorblind-mode', toggleState('colorblind'));
    };

    const toggleHighContrastMode = () => {
        updateBodyClass('set-high-contrast-mode', toggleState('highContrast'));
    };

    // Internal state map for less critical flags (optional, or just rely on body class toggling if we don't need reactive state for them elsewhere)
    const settingsState = ref<Record<string, boolean>>({});
    const toggleState = (key: string) => {
        settingsState.value[key] = !settingsState.value[key];
        return settingsState.value[key];
    };

    // Helpers
    const updateBodyClass = (className: string, add: boolean) => {
        if (add) {
            document.body.classList.add(className); // or document.documentElement depending on theme
            document.documentElement.classList.add(className);
        } else {
            document.body.classList.remove(className);
            document.documentElement.classList.remove(className);
        }
    };

    return {
        mobileMenuOpen,
        settingsDrawerOpen,
        appDrawerOpen,
        navMinified,
        toggleMobileMenu,
        closeMobileMenu,
        toggleSettingsDrawer,
        toggleAppDrawer,
        toggleNavMinified,
        filterText,
        toggleHeaderFixed,
        toggleNavFixed,
        toggleNavFull,
        toggleNavCollapsed,
        toggleColorblindMode,
        toggleHighContrastMode,
        settingsState
    };
});
