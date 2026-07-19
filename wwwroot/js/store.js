/**
 * CODENAME GAME - Vue Reactive Store
 * Pengganti gameState global object.
 * Semua komponen membaca/menulis store ini sebagai single source of truth.
 */
const store = Vue.reactive({
    player: { id: null, name: null },
    room:   { code: null, status: null, players: [], cards: [], state: {}, clues: [] },
    myRole: null,   // 'Spymaster' | 'FieldOperative' | null
    page:   'welcome'  // 'welcome' | 'lobby' | 'waiting' | 'game'
});

/* ── Helper: reset room state ── */
function resetRoom() {
    store.room   = { code: null, status: null, players: [], cards: [], state: {}, clues: [] };
    store.myRole = null;
}

/* ── Helper: sync role from server player list ── */
function syncMyRole(players) {
    if (!store.player.id) return;
    const me = players.find(p => p.id === store.player.id);
    if (me && me.gameRole && me.gameRole !== 'None') {
        store.myRole = me.gameRole;
    }
}

/* ── Helper: check if game over ── */
function isGameOver(room) {
    return room.status === 'Finished' || !!room.state?.winner;
}