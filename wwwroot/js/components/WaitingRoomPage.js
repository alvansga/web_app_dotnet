/**
 * CODENAME GAME - WaitingRoomPage Component
 * Halaman 3: Waiting room dengan role & team selection sebelum game dimulai
 */
const WaitingRoomPage = {
    template: `
        <div class="page active">
            <div class="waiting-container">
                <header class="page-header">
                    <h1>📋 Room: <span>{{ $store.room.code }}</span></h1>
                    <div class="header-actions">
                        <button class="btn-icon" title="Copy Room Code" @click="handleCopyRoomCode">📋</button>
                        <button class="btn-icon" title="Back to Lobby" @click="handleBackToLobby">⬅️</button>
                    </div>
                </header>

                <main class="waiting-main">
                    <!-- Players List -->
                    <section class="players-section">
                        <h2>👥 Players in Room</h2>
                        <ul class="players-list">
                            <li class="player-item" v-for="p in $store.room.players" :key="p.id" :class="playerItemClass(p)">
                                <span class="player-dot" :class="playerDotClass(p)"></span>
                                <span class="player-name">{{ p.name }}{{ p.id === $store.player.id ? ' (you)' : '' }}</span>
                                <span class="player-role-badge" :class="playerRoleBadgeClass(p)">{{ roleDisplay(p) }}</span>
                            </li>
                        </ul>
                        <p v-if="$store.room.players.length < 2" class="info-msg">⏳ Waiting for at least 2 players...</p>
                    </section>

                    <!-- Role Selection -->
                    <section class="role-selection-section">
                        <h2>🎭 Choose Your Role</h2>
                        <p class="info-msg" style="margin-bottom:12px;">Pick your team and role. Everyone can see what everyone picked.</p>
                        <div class="role-cards-grid">
                            <!-- Red Spymaster -->
                            <button
                                class="role-card role-card-red-sm"
                                :disabled="redSpymasterTaken || choosing"
                                @click="chooseRole('red-spymaster')"
                            >
                                <span class="role-icon-big">🕵️</span>
                                <span class="role-team-icon">🔴</span>
                                <span class="role-name">{{ redSpymasterTaken ? 'Red Spymaster (TAKEN)' : 'Red Spymaster' }}</span>
                                <span class="role-desc">{{ redSpymasterTaken ? 'Taken by ' + redSpymasterName : 'See red cards. Give clues to Red team.' }}</span>
                            </button>

                            <!-- Blue Spymaster -->
                            <button
                                class="role-card role-card-blue-sm"
                                :disabled="blueSpymasterTaken || choosing"
                                @click="chooseRole('blue-spymaster')"
                            >
                                <span class="role-icon-big">🕵️</span>
                                <span class="role-team-icon">🔵</span>
                                <span class="role-name">{{ blueSpymasterTaken ? 'Blue Spymaster (TAKEN)' : 'Blue Spymaster' }}</span>
                                <span class="role-desc">{{ blueSpymasterTaken ? 'Taken by ' + blueSpymasterName : 'See blue cards. Give clues to Blue team.' }}</span>
                            </button>

                            <!-- Red Field Operative -->
                            <button
                                class="role-card role-card-red-op"
                                :disabled="choosing"
                                @click="chooseRole('red-operative')"
                            >
                                <span class="role-icon-big">🧑‍💼</span>
                                <span class="role-team-icon">🔴</span>
                                <span class="role-name">Red Field Operative</span>
                                <span class="role-desc">Guess words based on Red Spymaster's clues. ({{ redOpCount }} player{{ redOpCount !== 1 ? 's' : '' }})</span>
                            </button>

                            <!-- Blue Field Operative -->
                            <button
                                class="role-card role-card-blue-op"
                                :disabled="choosing"
                                @click="chooseRole('blue-operative')"
                            >
                                <span class="role-icon-big">🧑‍💼</span>
                                <span class="role-team-icon">🔵</span>
                                <span class="role-name">Blue Field Operative</span>
                                <span class="role-desc">Guess words based on Blue Spymaster's clues. ({{ blueOpCount }} player{{ blueOpCount !== 1 ? 's' : '' }})</span>
                            </button>
                        </div>
                        <div v-if="roleMsg.text" class="message show" :class="roleMsg.type" style="margin-top:12px;">{{ roleMsg.text }}</div>
                        <p v-if="currentRole === 'None' || !currentRole" class="info-msg" style="margin-top:8px;">You haven't chosen a role yet — pick one above!</p>
                    </section>

                    <!-- Start Game Section -->
                    <section class="start-section">
                        <p v-if="!readyToStart" class="info-msg">⚠️ Need at least 1 Spymaster and 2+ players to start ({{ $store.room.players.length }}/2+ players)</p>
                        <p v-else class="info-msg" style="color:#4ade80;">✅ Ready to start! Spymaster(s) confirmed.</p>
                        <div class="spacer" style="height:28px;"></div>
                        <button
                            class="btn btn-primary"
                            :disabled="!readyToStart || starting"
                            :class="{ loading: starting }"
                            @click="handleStartGame"
                        >
                            <span v-if="!starting">🎯 Start Game</span>
                            <span v-else>Starting...</span>
                        </button>
                        <div v-if="startMsg.text" class="message show" :class="startMsg.type">{{ startMsg.text }}</div>
                    </section>

                </main>
            </div>
        </div>
    `,
    data() {
        return {
            poller: null,
            choosing: false,
            roleMsg: { text: '', type: '' },
            starting: false,
            startMsg: { text: '', type: '' },
        };
    },
    computed: {
        currentRole() {
            const me = (store.room.players || []).find(p => p.id === store.player.id);
            return me?.gameRole || 'None';
        },
        readyToStart() {
            return (this.redSpymasterTaken || this.blueSpymasterTaken) && store.room.players.length >= 2;
        },
        redSpymasterTaken() {
            return (store.room.players || []).some(p => p.gameRole === 'RedSpymaster');
        },
        redSpymasterName() {
            const sm = (store.room.players || []).find(p => p.gameRole === 'RedSpymaster');
            return sm ? sm.name : '';
        },
        blueSpymasterTaken() {
            return (store.room.players || []).some(p => p.gameRole === 'BlueSpymaster');
        },
        blueSpymasterName() {
            const sm = (store.room.players || []).find(p => p.gameRole === 'BlueSpymaster');
            return sm ? sm.name : '';
        },
        redOpCount() {
            return (store.room.players || []).filter(p => p.gameRole === 'RedFieldOperative').length;
        },
        blueOpCount() {
            return (store.room.players || []).filter(p => p.gameRole === 'BlueFieldOperative').length;
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
                if (room.status === 'Playing') {
                    const me = (room.players || []).find(p => p.id === store.player.id);
                    const serverRole = me?.gameRole;
                    if (serverRole && serverRole !== 'None') {
                        syncMyRole(room.players);
                        store.page = 'game';
                    }
                }
            } catch (err) {
                console.error('WaitingRoom poll error:', err);
            }
        },

        async chooseRole(role) {
            this.choosing = true;
            this.roleMsg = { text: '', type: '' };
            try {
                const code = store.room.code;
                const pid = store.player.id;

                if (role === 'red-spymaster') {
                    await api.assignRedSpymaster(code, pid);
                } else if (role === 'blue-spymaster') {
                    await api.assignBlueSpymaster(code, pid);
                } else if (role === 'red-operative') {
                    await api.assignRedFieldOperative(code, pid);
                } else if (role === 'blue-operative') {
                    await api.assignBlueFieldOperative(code, pid);
                }

                this.roleMsg = { text: '✅ Role assigned!', type: 'success' };
                await this.loadDetails();
            } catch (err) {
                this.roleMsg = { text: `❌ ${err.message}`, type: 'error' };
            } finally {
                this.choosing = false;
                setTimeout(() => { this.roleMsg = { text: '', type: '' }; }, 3000);
            }
        },

        async handleStartGame() {
            this.starting = true;
            this.startMsg = { text: '', type: '' };
            try {
                await api.startGame(store.room.code, store.player.id);
                this.startMsg = { text: '✅ Game started!', type: 'success' };
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
            navigator.clipboard.writeText(store.room.code).catch(err => {
                console.error('Copy failed:', err);
            });
        },

        stopPoller() {
            if (this.poller) { clearInterval(this.poller); this.poller = null; }
        },

        /* ── Display Helpers ── */
        roleDisplay(player) {
            switch (player.gameRole) {
                case 'RedSpymaster':      return '🕵️🔴 Red SM';
                case 'BlueSpymaster':     return '🕵️🔵 Blue SM';
                case 'RedFieldOperative': return '🔴 Red Op';
                case 'BlueFieldOperative': return '🔵 Blue Op';
                default:                  return '⏳ No Role';
            }
        },

        playerDotClass(player) {
            if (player.gameRole === 'RedSpymaster' || player.gameRole === 'RedFieldOperative') return 'dot-red';
            if (player.gameRole === 'BlueSpymaster' || player.gameRole === 'BlueFieldOperative') return 'dot-blue';
            return 'dot-none';
        },

        playerRoleBadgeClass(player) {
            if (player.gameRole === 'RedSpymaster') return 'badge-red-sm';
            if (player.gameRole === 'BlueSpymaster') return 'badge-blue-sm';
            if (player.gameRole === 'RedFieldOperative') return 'badge-red-op';
            if (player.gameRole === 'BlueFieldOperative') return 'badge-blue-op';
            return 'badge-none';
        },

        playerItemClass(player) {
            if (player.gameRole === 'RedSpymaster' || player.gameRole === 'RedFieldOperative') return 'player-red';
            if (player.gameRole === 'BlueSpymaster' || player.gameRole === 'BlueFieldOperative') return 'player-blue';
            return '';
        }
    }
};