/**
 * CODENAME GAME - GameBoardPage Component
 * Halaman 4: Board, team-based clue system, game over overlay, action log
 * Note: Role selection is done in WaitingRoomPage. No turn system — all field ops can click freely.
 */
const GameBoardPage = {
    template: `
        <div class="page active">
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
                        <div v-if="myTeamLabel" class="my-team-badge" :class="myTeamBadgeClass">{{ myTeamLabel }}</div>

                        <h3>👥 Players</h3>
                        <ul class="players-list">
                            <li v-for="p in $store.room.players" :key="p.id" class="player-item">
                                {{ p.name }}{{ p.id === $store.player.id ? ' (you)' : '' }}
                                <span class="player-role-tag" :class="playerRoleTagClass(p)">{{ roleTag(p) }}</span>
                            </li>
                        </ul>

                        <div class="clue-section">
                            <h3>💡 Clues</h3>
                            <div class="clue-list">
                                <div v-if="!$store.room.clues || $store.room.clues.length === 0" class="empty-message">No clues yet</div>
                                <div v-for="clue in $store.room.clues" :key="clue.id" class="clue-card" :class="clueTeamClass(clue)">
                                    <div class="clue-content">
                                        <span class="clue-team-tag" :class="clueTeamTagClass(clue)">{{ clue.spymasterTeam }}</span>
                                        <span class="clue-text">{{ clue.word }}</span>
                                        <span class="clue-count">{{ clue.count }}</span>
                                    </div>
                                    <button v-if="canManageClue(clue)" class="btn-remove-clue" title="Remove clue" @click="handleRemoveClue(clue.id)">✖</button>
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

                        <!-- === Action Log === -->
                        <div class="action-log-section">
                            <h3>📋 Action Log</h3>
                            <div class="action-log-list">
                                <div v-if="!$store.room.actionLogs || $store.room.actionLogs.length === 0" class="empty-message">No actions yet</div>
                                <div v-for="log in actionLogsSorted" :key="log.id" class="action-log-entry" :class="actionLogTeamClass(log)">
                                    <span class="log-player">{{ log.playerName }}</span>
                                    <span class="log-player-team" :class="actionLogTeamBadgeClass(log)">({{ log.team }})</span>
                                    <span class="log-action">membuka kata</span>
                                    <span class="log-word">{{ log.word }}</span>
                                    <span class="log-card-role" :class="cardRoleBadgeClass(log)">({{ formatCardRole(log.cardRole) }})</span>
                                </div>
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
            showGameOver: false,
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
            return store.myRole === 'RedSpymaster' || store.myRole === 'BlueSpymaster';
        },
        isFieldOperative() {
            return store.myRole === 'RedFieldOperative' || store.myRole === 'BlueFieldOperative';
        },
        myRoleLabel() {
            if (store.myRole === 'RedSpymaster') return '🕵️🔴 Red Spymaster';
            if (store.myRole === 'BlueSpymaster') return '🕵️🔵 Blue Spymaster';
            if (store.myRole === 'RedFieldOperative') return '🔴 Red Field Operative';
            if (store.myRole === 'BlueFieldOperative') return '🔵 Blue Field Operative';
            return '';
        },
        myRoleBadgeClass() {
            if (store.myRole === 'RedSpymaster') return 'spymaster-red';
            if (store.myRole === 'BlueSpymaster') return 'spymaster-blue';
            if (store.myRole === 'RedFieldOperative') return 'operative-red';
            if (store.myRole === 'BlueFieldOperative') return 'operative-blue';
            return '';
        },
        myTeamLabel() {
            if (store.myTeam === 'Red') return '🔴 Red Team';
            if (store.myTeam === 'Blue') return '🔵 Blue Team';
            return '';
        },
        myTeamBadgeClass() {
            if (store.myTeam === 'Red') return 'team-red';
            if (store.myTeam === 'Blue') return 'team-blue';
            return '';
        },
        phaseLabel() {
            const p = store.room.state?.phase || '';
            if (p === 'Playing') return '🎮 In Progress';
            if (p === 'Ended') return '🏁 Finished';
            return p;
        },
        actionLogsSorted() {
            const logs = store.room.actionLogs || [];
            return [...logs].reverse(); // newest first
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
                    this.stopPoller();
                    resetRoom();
                    store.page = 'lobby';
                    return;
                }

                store.room.code        = room.code;
                store.room.status      = room.status;
                store.room.players     = room.players || [];
                store.room.cards       = room.cards || [];
                store.room.state       = room.state || {};
                store.room.clues       = room.clues || [];
                store.room.actionLogs  = room.actionLogs || [];

                syncMyRole(room.players);

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
            if (this.showGameOver) return true;
            if (!this.isFieldOperative) return true; // Only field ops can click
            // No turn restriction — all field operatives can click freely
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
            if (player.gameRole === 'RedSpymaster') return '🕵️🔴 SM';
            if (player.gameRole === 'BlueSpymaster') return '🕵️🔵 SM';
            if (player.gameRole === 'RedFieldOperative') return '🔴 Op';
            if (player.gameRole === 'BlueFieldOperative') return '🔵 Op';
            return '⏳';
        },
        playerRoleTagClass(player) {
            if (player.gameRole === 'RedSpymaster' || player.gameRole === 'RedFieldOperative') return 'tag-red';
            if (player.gameRole === 'BlueSpymaster' || player.gameRole === 'BlueFieldOperative') return 'tag-blue';
            return 'tag-none';
        },

        /* ── Clue System ── */
        adjustClueCount(delta) {
            let val = this.clueCount + delta;
            if (val < 0) val = 0;
            if (val > 9) val = 9;
            this.clueCount = val;
        },
        async handleSetClue() {
            if (!this.isSpymaster) {
                this.clueError = 'Only Spymasters can give clues.';
                setTimeout(() => { this.clueError = ''; }, 4000);
                return;
            }

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
        canManageClue(clue) {
            if (!this.isSpymaster) return false;
            if (!clue.spymasterTeam || !store.myTeam) return false;
            return clue.spymasterTeam === store.myTeam;
        },
        clueTeamClass(clue) {
            if (clue.spymasterTeam === 'Red') return 'clue-team-red';
            if (clue.spymasterTeam === 'Blue') return 'clue-team-blue';
            return '';
        },
        clueTeamTagClass(clue) {
            if (clue.spymasterTeam === 'Red') return 'team-tag-red';
            if (clue.spymasterTeam === 'Blue') return 'team-tag-blue';
            return '';
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

        /* ── Action Log ── */
        actionLogTeamClass(log) {
            if (log.team === 'Red') return 'log-entry-red';
            if (log.team === 'Blue') return 'log-entry-blue';
            return '';
        },
        actionLogTeamBadgeClass(log) {
            if (log.team === 'Red') return 'log-badge-red';
            if (log.team === 'Blue') return 'log-badge-blue';
            return '';
        },
        formatCardRole(cardRole) {
            if (cardRole === 'RedAgent') return 'RED';
            if (cardRole === 'BlueAgent') return 'BLUE';
            if (cardRole === 'Assassin') return 'ASSASSIN';
            if (cardRole === 'Bystander') return 'BYSTANDER';
            return cardRole;
        },
        cardRoleBadgeClass(log) {
            if (log.cardRole === 'RedAgent') return 'role-badge-red';
            if (log.cardRole === 'BlueAgent') return 'role-badge-blue';
            if (log.cardRole === 'Assassin') return 'role-badge-assassin';
            if (log.cardRole === 'Bystander') return 'role-badge-bystander';
            return '';
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
            store.page = 'waiting';
        },
        stopPoller() {
            if (this.poller) { clearInterval(this.poller); this.poller = null; }
        }
    }
};