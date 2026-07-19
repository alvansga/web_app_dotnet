/**
 * CODENAME GAME - Vue App Entry Point
 * Initializes Vue 3 app, registers all components, manages page routing.
 */
const app = Vue.createApp({
    computed: {
        currentPageComponent() {
            const map = {
                'welcome':  'welcome-page',
                'lobby':    'lobby-page',
                'waiting':  'waiting-room-page',
                'game':     'game-board-page'
            };
            return store.page ? map[store.page] : 'welcome-page';
        }
    },
    template: `
        <component :is="currentPageComponent" />
    `
});

/* ── Make store accessible in all component templates as `$store` ── */
app.config.globalProperties.$store = store;

/* ── Register components ── */
app.component('welcome-page', WelcomePage);
app.component('lobby-page', LobbyPage);
app.component('waiting-room-page', WaitingRoomPage);
app.component('game-board-page', GameBoardPage);

/* ── Mount ── */
app.mount('#app');