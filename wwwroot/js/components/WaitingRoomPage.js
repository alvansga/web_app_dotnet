/**
 * CODENAME GAME - WaitingRoomPage Component
 * Halaman 3: Waiting room sebelum game dimulai, player list, start game
 */
const WaitingRoomPage = {
    template: `
        <div class="page active">
            <div class="waiting-container">
                <header class="page-header">
                    <h1>Room: <span>{{ $store.room.code }}</span></h1>
                    <button class="btn-icon" title="Back to Lobby" @click="handleBackToLobby">⬅️</button>
                </header>

                <main class="waiting-main">
                    <section class="players-section">
                        <h2>Players in Room</h2>
                        <ul class="players-list">
                            <li class="player-item" v-for="p in $store.room.players" :key="p.id">
                                {{ p.name }}
                            </li>
                        </ul>
                        <p v-if="$store.room.players.length < 2" class="info-msg">⏳ Waiting for at least 2 players...</p>
                    </section>

                    <section class="start-section">
                        <button
                            class="btn btn-primary"
                            :disabled="!canStart || starting"
                            :class="{ loading: starting }"
                            @click="handleStartGame"
                        >
                            {{ starting ? 'Starting...' : 'Start Game' }}
                        </button>
                        <p v-if="!canStart" class="info-msg">{{ $store.room.players.length }}/2 players</p>
                        <p v-else class="info-msg" style="color:var(--success)">✅ Ready to start!</p>
                        <div v-if="startMsg.text" class="message show" :class="startMsg.type">{{ startMsg.text }}</div>
                    </section>

                    <button class="btn btn-secondary" @click="handleCopyRoomCode">{{ copyBtnText }}</button>
                </main>
            </div>
        </div>
    `,
    data() {
        return {
            poller: null,
            starting: false,
            startMsg: { text: '', type: '' },
            copyBtnText: '📋 Copy Room Code'
        };
    },
    computed: {
        canStart() {
            return store.room.players.length >= 2;
        }
    },
    mounted() {
        this.loadDetails();
        this.poller = setInterval(() => this.loadDetails(), 2000);
    },
    beforeUnmount() {
        if (this.poller) { clearInterval(this.poller); this.poller = null; }
    },
    methods: {
        async loadDetails() {
            try {
                const room = await api.getRoomDetail(store.room.code, store.player.id);
                if (!room) {
                    // Room deleted or not found — stop polling, go back to lobby
                    this.stopPoller();
                    resetRoom();
                    store.page = 'lobby';
                    return;
                }

                store.room.code     = room.code;
                store.room.status   = room.status;
                store.room.players  = room.players || [];
                store.room.cards    = room.cards || [];
                store.room.state    = room.state || {};

                // Detect transition: game started by someone else
                if (room.status === 'Playing' || room.status === 'Finished') {
                    const me = (room.players || []).find(p => p.id === store.player.id);
                    const serverRole = me?.gameRole;
                    if (serverRole && serverRole !== 'None') {
                        store.myRole = serverRole;
                        store.page = 'game';
                    }
                }

                // Detect if game over while in waiting room
                if (isGameOver(room)) {
                    store.page = 'game';
                }
            } catch (err) {
                console.error('WaitingRoom poll error:', err);
            }
        },
        async handleStartGame() {
            this.starting = true;
            this.startMsg = { text: '', type: '' };
            try {
                await api.startGame(store.room.code, store.player.id);
                this.startMsg = { text: '✅ Game started!', type: 'success' };
                // Poller will pick up the role and redirect
            } catch (err) {
                this.startMsg = { text: `❌ ${err.message}`, type: 'error' };
                this.starting = false;
            }
        },
        async handleBackToLobby() {
            if (!confirm('Are you sure? You will leave the room.')) return;
            try {
                await api.leaveRoom(store.player.id);
            } catch (err) {
                console.error('Failed to notify server:', err);
            }
            resetRoom();
            store.page = 'lobby';
        },
        handleCopyRoomCode() {
            navigator.clipboard.writeText(store.room.code).then(() => {
                this.copyBtnText = '✓ Copied!';
                setTimeout(() => { this.copyBtnText = '📋 Copy Room Code'; }, 2000);
            }).catch(() => {
                this.copyBtnText = '⚠ Copy failed';
                setTimeout(() => { this.copyBtnText = '📋 Copy Room Code'; }, 2000);
            });
        },
        stopPoller() {
            if (this.poller) { clearInterval(this.poller); this.poller = null; }
        }
    }
};