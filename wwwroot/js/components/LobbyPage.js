/**
 * CODENAME GAME - LobbyPage Component
 * Halaman 2: Create/Join room + available rooms list + logout
 */
const LobbyPage = {
    template: `
        <div class="page active">
            <div class="lobby-container">
                <header class="page-header">
                    <h1>🎮 Game Lobby</h1>
                    <div class="player-info">
                        <span>👋 {{ $store.player.name }}</span>
                        <button class="btn-icon" title="Logout" @click="handleLogout">🚪</button>
                    </div>
                </header>
                <div class="online-stats-bar">
                    <span class="stat-item">🟢 {{ stats.totalOnline }} online</span>
                    <span class="stat-sep">•</span>
                    <span class="stat-item">🏠 {{ stats.inLobby }} di lobby</span>
                    <span class="stat-sep">•</span>
                    <span class="stat-item">🎯 {{ stats.playing }} bermain</span>
                </div>

                <main class="lobby-main">
                    <div class="lobby-content">
                        <!-- Create Room -->
                        <section class="lobby-section">
                            <h2>Create a New Room</h2>
                            <button class="btn btn-primary" :disabled="creating" :class="{ loading: creating }" @click="handleCreateRoom">
                                <span v-if="!creating">✨ Create Room</span>
                                <span v-else>Creating...</span>
                            </button>
                            <div v-if="createMsg.text" class="message show" :class="createMsg.type">{{ createMsg.text }}</div>
                        </section>

                        <!-- Join Room -->
                        <section class="lobby-section">
                            <h2>Join Existing Room</h2>
                            <div class="join-room-form">
                                <input
                                    type="text"
                                    v-model="roomCode"
                                    placeholder="Enter room code (e.g. ABC123)"
                                    maxlength="6"
                                    @keypress.enter="handleJoinRoom"
                                >
                                <button class="btn btn-secondary" :disabled="joining" :class="{ loading: joining }" @click="handleJoinRoom">
                                    <span v-if="!joining">🔗 Join</span>
                                    <span v-else>Joining...</span>
                                </button>
                            </div>
                            <div v-if="joinMsg.text" class="message show" :class="joinMsg.type">{{ joinMsg.text }}</div>
                        </section>

                        <!-- Available Rooms -->
                        <section class="lobby-section">
                            <h2>Available Rooms</h2>
                            <div class="rooms-list">
                                <div v-if="rooms.length === 0" class="empty-message">
                                    <div style="font-size:2em;margin-bottom:8px;">📭</div>
                                    No rooms available — create one to get started!
                                </div>
                                <div
                                    v-for="room in rooms"
                                    :key="room.code"
                                    class="room-item"
                                    @click="quickJoin(room.code)"
                                >
                                    <div class="room-details">
                                        <h3>🏠 Room {{ room.code }}</h3>
                                        <p>{{ room.players.length }} player{{ room.players.length !== 1 ? 's' : '' }} waiting</p>
                                    </div>
                                    <div class="room-players">Join →</div>
                                </div>
                            </div>
                        </section>
                    </div>
                </main>
            </div>
        </div>
    `,
    data() {
        return {
            roomCode: '',
            creating: false,
            joining: false,
            createMsg: { text: '', type: '' },
            joinMsg: { text: '', type: '' },
            rooms: [],
            stats: { totalOnline: 0, inLobby: 0, playing: 0, totalRooms: 0 },
            poller: null,
            statsPoller: null
        };
    },
    mounted() {
        this.loadRooms();
        this.loadStats();
        this.poller = setInterval(() => this.loadRooms(), 3000);
        this.statsPoller = setInterval(() => this.loadStats(), 5000);
    },
    beforeUnmount() {
        if (this.poller) { clearInterval(this.poller); this.poller = null; }
        if (this.statsPoller) { clearInterval(this.statsPoller); this.statsPoller = null; }
    },
    methods: {
        async loadRooms() {
            try {
                const allRooms = await api.getRooms();
                this.rooms = allRooms.filter(r => r.status === 'Waiting');
            } catch {
                // Silently fail, rooms will refresh next poll
            }
        },
        async loadStats() {
            try {
                this.stats = await api.getOnlineStats();
            } catch {
                // Silently fail, stats will refresh next poll
            }
        },
        async handleCreateRoom() {
            this.creating = true;
            this.createMsg = { text: '', type: '' };
            try {
                const data = await api.createRoom();
                const code = data.code;
                await api.joinRoom(code, store.player.id);
                store.room.code = code;
                this.createMsg = { text: `✅ Room created & joined! Code: ${code}`, type: 'success' };
                setTimeout(() => { store.page = 'waiting'; }, 500);
            } catch (err) {
                this.createMsg = { text: `❌ ${err.message}`, type: 'error' };
            } finally {
                this.creating = false;
            }
        },
        async handleJoinRoom() {
            const code = this.roomCode.trim().toUpperCase();
            if (!code) {
                this.joinMsg = { text: 'Please enter room code', type: 'error' };
                return;
            }
            this.joining = true;
            this.joinMsg = { text: '', type: '' };
            try {
                await api.joinRoom(code, store.player.id);
                store.room.code = code;
                this.joinMsg = { text: '✅ Joined room!', type: 'success' };
                setTimeout(() => { store.page = 'waiting'; }, 500);
            } catch (err) {
                this.joinMsg = { text: `❌ ${err.message}`, type: 'error' };
            } finally {
                this.joining = false;
            }
        },
        quickJoin(code) {
            this.roomCode = code;
            this.handleJoinRoom();
        },
        handleLogout() {
            sessionStorage.removeItem('player');
            store.player = { id: null, name: null, token: null };
            resetRoom();
            store.page = 'welcome';
        }
    }
};