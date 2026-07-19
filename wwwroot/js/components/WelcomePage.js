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
                    <p class="subtitle">Team word-guessing game</p>

                    <div class="welcome-form">
                        <input
                            type="text"
                            v-model="playerName"
                            placeholder="Enter your name"
                            maxlength="20"
                            @keypress.enter="handleCreatePlayer"
                        >
                        <button class="btn btn-primary" :disabled="loading" :class="{ loading }" @click="handleCreatePlayer">
                            {{ loading ? 'Entering...' : 'Enter Game' }}
                        </button>
                        <div v-if="message.text" class="message show" :class="message.type">{{ message.text }}</div>
                    </div>

                    <div class="rules">
                        <h3>How to Play</h3>
                        <ul>
                            <li>Create a room or join with a code</li>
                            <li>Wait for players to join</li>
                            <li>One team gives clues, others guess words</li>
                            <li>Find all your team's words to win</li>
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