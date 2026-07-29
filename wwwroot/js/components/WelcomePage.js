/**
 * CODENAME GAME - WelcomePage Component
 * Halaman 1: Input nama pemain + rules
 */
const WelcomePage = {
    template: `
        <div class="page active">
            <div class="welcome-container">
                <div class="welcome-content">
                    <h1>🔐 CODENAME</h1>
                    <p class="subtitle">The ultimate word-guessing game for teams</p>

                    <div class="welcome-form">
                        <input
                            type="text"
                            v-model="playerName"
                            placeholder="Enter your display name"
                            maxlength="20"
                            @keypress.enter="handleCreatePlayer"
                            autofocus
                        >
                        <button class="btn btn-primary" :disabled="loading" :class="{ loading }" @click="handleCreatePlayer">
                            <span v-if="!loading">🚀 Enter Game</span>
                            <span v-else>Entering...</span>
                        </button>
                        <div v-if="message.text" class="message show" :class="message.type">{{ message.text }}</div>
                    </div>

                    <div class="rules">
                        <h3>📖 How to Play</h3>
                        <ul>
                            <li>Create a new room or join with a room code</li>
                            <li>Wait for at least 2 players to join</li>
                            <li>Spymaster gives one-word clues + a number</li>
                            <li>Field operatives guess the words on the board</li>
                            <li>Find all your team's agents to win — but avoid the assassin!</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    `,
    data() {
        return {
            playerName: '',
            loading: false,
            message: { text: '', type: '' }
        };
    },
    methods: {
        async handleCreatePlayer() {
            const name = this.playerName.trim();
            if (!name || name.length < 2) {
                this.message = { text: 'Name must be at least 2 characters', type: 'error' };
                return;
            }

            this.loading = true;
            this.message = { text: '', type: '' };
            try {
                const data = await api.createPlayer(name);
                store.player = { id: data.id, name };
                localStorage.setItem('player', JSON.stringify(store.player));
                this.message = { text: '✅ Welcome! Redirecting...', type: 'success' };
                setTimeout(() => { store.page = 'lobby'; }, 1000);
            } catch (err) {
                this.message = { text: `❌ ${err.message}`, type: 'error' };
            } finally {
                this.loading = false;
            }
        }
    }
};