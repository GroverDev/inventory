export default {
    name: 'public_access',
    component: () => import(/* webpackChunkName: "public_access" */ '@/modules/common/layout/CommonLayout.vue'),
    children: [
        {
            path: '',
            name: 'public_dashboard',
            component: () => import(/* webpackChunkName: "main-inventory" */ '@/modules/common/views/HomeView.vue'),
            //beforeEnter: [isAuthenticatedGuard],
            meta: {
                title: 'Principal',
                titleForm: 'Principal',
            },
        },
        {
            path: '/:pathMatch(.*)*', // Catch-all route for 404s or unhandled paths
            name: 'NotFound',
            component: () => import(/* webpackChunkName: "main-inventory" */ '@/modules/common/views/NotFoundView.vue'),// Crea un componente NotFound simple
        }
    ],
};
