//Run gobal scripts: after all other scripts are loaded
/* Initialize the navigation : smartNavigation.js */
 // se comento la linea del 23 al 45 de smartApp.js y se pego en este archivo

let nav;
const navElement = document.querySelector('#js-primary-nav');
if (navElement) {
    nav = new Navigation(navElement,
        {
            accordion: true,
            slideUpSpeed: 350,
            slideDownSpeed: 470,
            closedSign: '<i class="sa sa-chevron-down"></i>',
            openedSign: '<i class="sa sa-chevron-up"></i>',
            initClass: 'js-nav-built',
            debug: false,
            instanceId: `nav-${Date.now()}`,
            maxDepth: 5,
            sanitize: true,
            animationTiming: 'easeOutExpo',
            debounceTime: 0,
            onError: error => console.error('Navigation error:', error)
        });
}
