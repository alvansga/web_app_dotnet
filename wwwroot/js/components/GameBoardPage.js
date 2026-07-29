/**
 * CODENAME GAME - GameBoardPage Component
 * Halaman 4: Board, role modal, clue system, game over overlay
 */
const GameBoardPage = {
    template: `
        <div class="page active">
            <!-- Role Selection Modal -->
            <div v-if="showRoleModal" class="modal-overlay" style="display:flex;">
                <div class="modal-box">
                    <h2>🎭 Choose Your Role</h2>
                    <p class="modal-subtitle">The game has started! Pick your role for this round.</p>
                    <div class="role-cards">
                        <button
                            class="role-card role-spymaster"
                            :disabled="spymasterTaken || choosing"
                            @click="chooseRole('spymaster')"
                        >
                            <span class="role-icon">🕵️</span>
                            <span class="role-name">{{ spymasterTaken ? 'Spymaster (TAKEN)' : 'Spymaster' }}</span>
                            <span class="role-desc">{{ spymasterTaken ? 'Already taken by ' + spymasterName : 'See all card colors. Give one-word clues to guide your team.' }}</span>
                        </button>
                        <button
                            class="role-card role-field"
                            :disabled="choosing"
                            @click="chooseRole('field-operative')"
                        >
                            <span class="role-icon">🧑‍💼</span>
                            <span class="role-name">Field Operative</span>
                            <span class="role-desc">Guess words based on your Spymaster's clues. ({{ fieldOpCount }} player{{ fieldOpCount !== 1 ? 's' : '' }})</span>
                        </button>
                    </div>
                    <div v-if="roleMsg.text" class="message show" :class="roleMsg.type">{{ roleMsg.text }}</div>
                    <p v-if="!$store.myRole" class="info-msg" style="margin-top:12px;">Waiting for all players to choose roles...</p>
                </div>
            </div>

            <!-- Game Over Overlay -->
            <div v-if="showGameOver" class="modal-overlay" style="display:flex;">
                <div class="modal-box game-over-box" :class="gameOverClass">
                    <div class="game-over-icon">{{ gameOverIcon }}</div>
                    <h2 class="game-over-title">{{ gameOverTitle }}</h2>
                    <p class="game-over-desc">{{ gameOverDesc }}</p>
                    <button class="btn btn-primary" @click="backToLobby">🏠 Back to Lobby</button>
                </div>
            </div>

            <!-- Game Board -->
            <div class="game-container">
                <header class="game-header">
                    <div class="game-info">
                        <h1>🎯 Room: <span>{{ $store.room.code }}</span></h1>
                        <div class="game-stats">
                            <span>🔄 Round {{ $store.room.state?.round || 1 }}</span>
                            <span v-if="phaseLabel" class="phase-badge">{{ phaseLabel }}</span>
                        </div>
                    </div>
                </header>

                <main class="game-main">
                    <div class="cards-grid">
                        <button
                            v-for="card in $store.room.cards"
                            :key="card.id"
                            class="card"
                            :class="cardClasses(card)"
                            :disabled="isCardDisabled(card)"
                            @click="handleRevealCard(card, $event)"
                            :title="isCardDisabled(card) ? '' : 'Click to reveal'"
                        >
                            {{ card.word }}
                        </button>
                    </div>
                    <div v-if="revealError" class="message show error" style="margin-top:8px;">{{ revealError }}</div>

                    <section class="game-info-panel">
                        <div v-if="myRoleLabel" class="my-role-badge" :class="myRoleBadgeClass">{{ myRoleLabel }}</div>

                        <h3>👥 Players</h3>
                        <ul class="players-list">
                            <li v-for="p in $store.room.players" :key="p.id" class="player-item">
                                {{ p.name }}{{ p.id === $store.player.id ? ' (you)' : '' }}
                                <span class="player-role-tag">{{ roleTag(p) }}</span>
                            </li>
                        </ul>

                        <div class="clue-section">
                            <h3>💡 Clues</h3>
                            <div class="clue-list">
                                <div v-if="!$store.room.clues || $store.room.clues.length === 0" class="empty-message">No clues yet</div>
                                <div v-for="clue in $store.room.clues" :key="clue.id" class="clue-card">
                                    <div>
                                        <span class="clue-text">{{ clue.word }}</span>
                                        <span class="clue-count">{{ clue.count }}</span>
                                    </div>
                                    <button v-if="isSpymaster" class="btn-remove-clue" title="Remove clue" @click="handleRemoveClue(clue.id)">✖</button>
                                </div>
                            </div>

                            <div v-if="isSpymaster" class="spymaster-form">
                                <div class="clue-inputs">
                                    <input
                                        type="text"
                                        v-model="clueWord"
                                        placeholder="Clue word..."
                                        class="clue-input-text"
                                        @keypress.enter="handleSetClue"
                                    >
                                    <div class="clue-count-control">
                                        <button class="btn-count" @click="adjustClueCount(-1)">−</button>
                                        <input type="number" v-model.number="clueCount" min="0" max="9" readonly>
                                        <button class="btn-count" @click="adjustClueCount(1)">+</button>
                                    </div>
                                    <button class="btn-add-clue" title="Add Clue" @click="handleSetClue">+</button>
                                </div>
                                <div v-if="clueError" class="message show error" style="margin-top:8px;">{{ clueError }}</div>
                            </div>
                        </div>
                    </section>
                </main>
            </div>
        </div>
    `,
    data() {
        return {
            poller: null,
            showRoleModal: false,
            showGameOver: false,
            choosing: false,
            roleMsg: { text: '', type: '' },
            clueWord: '',
            clueCount: 1,
            gameOverIcon: '🏁',
            gameOverTitle: 'Game Over!',
            gameOverDesc: '',
            gameOverClass: '',
            revealError: '',
            clueError: ''
        };
    },
    computed: {
        isSpymaster() {
            return store.myRole === 'Spymaster';
        },
        isFieldOperative() {
            return store.myRole === 'FieldOperative';
        },
        myRoleLabel() {
            if (store.myRole === 'Spymaster') return '🕵️ You are the Spymaster';
            if (store.myRole === 'FieldOperative') return '🧑‍💼 You are a Field Operative';
            return '';
        },
        myRoleBadgeClass() {
            if (store.myRole === 'Spymaster') return 'spymaster';
            if (store.myRole === 'FieldOperative') return 'field-operative';
            return '';
        },
        phaseLabel() {
            const p = store.room.state?.phase || '';
            if (p === 'SpymasterSelection') return '🎭 Choosing Roles';
            if (p === 'Playing') return '🎮 In Progress';
            if (p === 'Ended') return '🏁 Finished';
            return p;
        },
        spymasterTaken() {
            return (store.room.players || []).some(p => p.gameRole === 'Spymaster');
        },
        spymasterName() {
            const sm = (store.room.players || []).find(p => p.gameRole === 'Spymaster');
            return sm ? sm.name : '';
        },
        fieldOpCount() {
            return (store.room.players || []).filter(p => p.gameRole === 'FieldOperative').length;
        }
    },
    mounted() {
        this.loadBoard();
        this.poller = setInterval(() => this.loadBoard(), 2500);
    },
    beforeUnmount() {
        if (this.poller) { clearInterval(this.poller); this.poller = null; }
    },
    methods: {
        async loadBoard() {
            try {
                const room = await api.getRoomDetail(store.room.code, store.player.id);
                if (!room) {
                    // Room deleted or not found — stop polling, go back to lobby
                    this.stopPoller();
                    resetRoom();
                    store.page = 'lobby';
                    return;
                }

                store.room.code    = room.code;
                store.room.status  = room.status;
                store.room.players = room.players || [];
                store.room.cards   = room.cards || [];
                store.room.state   = room.state || {};
                store.room.clues   = room.clues || [];

                syncMyRole(room.players);

                const me = (room.players || []).find(p => p.id === store.player.id);
                const hasNoRole = !me || !me.gameRole || me.gameRole === 'None';
                const isPlaying = room.status === 'Playing' || room.status === 'Finished';

                if (isPlaying && hasNoRole) {
                    this.showRoleModal = true;
                } else if (store.myRole && store.myRole !== 'None') {
                    this.showRoleModal = false;
                }

                // Game over detection
                if (isGameOver(room)) {
                    this.showGameOver = true;
                    this.setGameOverDisplay(room.state?.winner);
                    this.stopPoller();
                }
            } catch (err) {
                console.error('GameBoard poll error:', err);
            }
        },
        cardClasses(card) {
            const classes = [];
            if (card.isRevealed) {
                classes.push('revealed');
                if (card.role === 'RedAgent') classes.push('red-agent');
                if (card.role === 'BlueAgent') classes.push('blue-agent');
                if (card.role === 'Bystander') classes.push('bystander');
                if (card.role === 'Assassin') classes.push('assassin');
            } else if (this.isSpymaster && card.role) {
                // Hint colors for spymaster (unrevealed cards)
                if (card.role === 'RedAgent') classes.push('hint-red');
                if (card.role === 'BlueAgent') classes.push('hint-blue');
                if (card.role === 'Bystander') classes.push('hint-bystander');
                if (card.role === 'Assassin') classes.push('hint-assassin');
            }
            return classes;
        },
        isCardDisabled(card) {
            if (card.isRevealed) return true;
            if (this.isSpymaster) return true; // Spymaster can't click cards
            if (this.showRoleModal || this.showGameOver) return true;
            return false;
        },
        async handleRevealCard(card, event) {
            if (card.isRevealed) return;

            if (!this.isFieldOperative) {
                this.revealError = `Only Field Operatives can reveal cards. You are: ${store.myRole || 'None'}.`;
                setTimeout(() => { this.revealError = ''; }, 4000);
                return;
            }

            const el = event.target;
            el.disabled = true;
            el.classList.add('loading');
            try {
                await api.revealCard(store.room.code, card.id, store.player.id);
                await this.loadBoard();
            } catch (err) {
                console.error('RevealCard error:', err);
                this.revealError = `❌ Error: ${err.message}`;
                setTimeout(() => { this.revealError = ''; }, 4000);
                el.disabled = false;
                el.classList.remove('loading');
            }
        },
        roleTag(player) {
            if (player.gameRole === 'Spymaster') return '🕵️';
            if (player.gameRole === 'FieldOperative') return '🧑‍💼';
            return '⏳';
        },

        /* ── Role Modal ── */
        async chooseRole(role) {
            this.choosing = true;
            this.roleMsg = { text: '', type: '' };
            try {
                if (role === 'spymaster') {
                    await api.assignSpymaster(store.room.code, store.player.id);
                    store.myRole = 'Spymaster';
                } else {
                    await api.assignFieldOperative(store.room.code, store.player.id);
                    store.myRole = 'FieldOperative';
                }
                this.roleMsg = {
                    text: `✅ You are now the ${role === 'spymaster' ? 'Spymaster 🕵️' : 'Field Operative 🧑‍💼'}!`,
                    type: 'success'
                };
                setTimeout(() => { this.showRoleModal = false; }, 900);
            } catch (err) {
                this.roleMsg = { text: `❌ ${err.message}`, type: 'error' };
            } finally {
                this.choosing = false;
            }
        },

        /* ── Clue System ── */
        adjustClueCount(delta) {
            let val = this.clueCount + delta;
            if (val < 0) val = 0;
            if (val > 9) val = 9;
            this.clueCount = val;
        },
        async handleSetClue() {
            const word = this.clueWord.trim();
            if (!word || word.includes(' ')) {
                this.clueError = 'Please enter a single word.';
                setTimeout(() => { this.clueError = ''; }, 4000);
                return;
            }
            try {
                await api.addClue(store.room.code, store.player.id, word, this.clueCount);
                this.clueWord = '';
                this.clueCount = 1;
                this.clueError = '';
                await this.loadBoard();
            } catch (err) {
                console.error('Failed to set clue:', err);
                this.clueError = err.message || 'Failed to add clue';
                setTimeout(() => { this.clueError = ''; }, 4000);
            }
        },
        async handleRemoveClue(clueId) {
            if (!confirm('Remove this clue?')) return;
            try {
                await api.removeClue(store.room.code, clueId, store.player.id);
                await this.loadBoard();
            } catch (err) {
                console.error('Failed to remove clue:', err);
            }
        },

        /* ── Game Over ── */
        setGameOverDisplay(winner) {
            this.gameOverClass = '';
            if (winner === 'Assassin') {
                this.gameOverClass = 'winner-assassin';
                this.gameOverIcon = '💀';
                this.gameOverTitle = 'Assassin Revealed!';
                this.gameOverDesc = 'The Assassin card was revealed. Game over!';
            } else if (winner === 'RedTeam') {
                this.gameOverClass = 'winner-red';
                this.gameOverIcon = '🔴';
                this.gameOverTitle = 'Red Team Wins!';
                this.gameOverDesc = 'All Red Agent cards have been revealed. Red Team is victorious!';
            } else if (winner === 'BlueTeam') {
                this.gameOverClass = 'winner-blue';
                this.gameOverIcon = '🔵';
                this.gameOverTitle = 'Blue Team Wins!';
                this.gameOverDesc = 'All Blue Agent cards have been revealed. Blue Team is victorious!';
            } else {
                this.gameOverIcon = '🏁';
                this.gameOverTitle = 'Game Over!';
                this.gameOverDesc = '';
            }
        },
        backToLobby() {
            this.showGameOver = false;
            this.stopPoller();
            resetRoom();
            store.page = 'lobby';
        },
        stopPoller() {
            if (this.poller) { clearInterval(this.poller); this.poller = null; }
        }
    }
};