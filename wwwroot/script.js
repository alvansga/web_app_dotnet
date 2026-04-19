/**
 * CODENAME GAME - JavaScript Controller
 * ======================================
 * Sections:
 * 1. Config & State
 * 2. DOM Elements
 * 3. Event Listeners
 * 4. Initialization
 * 5. Page Navigation
 * 6. Player Management
 * 7. Room Management
 * 8. Gameplay
 * 9. Utilities
 */

/* ============================================
   1. CONFIG & STATE
   ============================================ */
const API_BASE_URL      = '/api';
const WAITING_POLL_MS   = 2000;   // polling di waiting room
const GAME_POLL_MS      = 2500;   // polling di game page

let gameState = {
    player: { id: null, name: null },
    room:   { code: null, status: null, players: [], cards: [], state: null },
    myRole: null   // 'Spymaster' | 'FieldOperative' | null
};

let waitingPoller = null;   // polling waiting room
let gamePoller    = null;   // polling game board

/* ============================================
   2. DOM ELEMENTS
   ============================================ */
const welcomePage        = document.getElementById('welcomePage');
const playerNameInput    = document.getElementById('playerNameInput');
const startBtn           = document.getElementById('startBtn');
const welcomeMessage     = document.getElementById('welcomeMessage');

const lobbyPage          = document.getElementById('lobbyPage');
const playerNameDisplay  = document.getElementById('playerNameDisplay');
const logoutBtn          = document.getElementById('logoutBtn');
const createRoomBtn      = document.getElementById('createRoomBtn');
const createRoomMessage  = document.getElementById('createRoomMessage');
const roomCodeInput      = document.getElementById('roomCodeInput');
const joinRoomBtn        = document.getElementById('joinRoomBtn');
const joinRoomMessage    = document.getElementById('joinRoomMessage');
const availableRoomsList = document.getElementById('availableRoomsList');

const waitingRoomPage    = document.getElementById('waitingRoomPage');
const roomCodeDisplay    = document.getElementById('roomCodeDisplay');
const backToLobbyBtn     = document.getElementById('backToLobbyBtn');
const playersList        = document.getElementById('playersList');
const minPlayersMsg      = document.getElementById('minPlayersMsg');
const startGameBtn       = document.getElementById('startGameBtn');
const startGameInfo      = document.getElementById('startGameInfo');
const copyRoomCodeBtn    = document.getElementById('copyRoomCodeBtn');

const gamePage           = document.getElementById('gamePage');
const gameRoomCode       = document.getElementById('gameRoomCode');
const gameRound          = document.getElementById('gameRound');
const gameStatus         = document.getElementById('gameStatus');
const gamePhase          = document.getElementById('gamePhase');
const cardsGrid          = document.getElementById('cardsGrid');
const gamePlayers        = document.getElementById('gamePlayers');
const myRoleBadge        = document.getElementById('myRoleBadge');

const roleModal          = document.getElementById('roleModal');
const chooseSpymasterBtn = document.getElementById('chooseSpymasterBtn');
const chooseFieldBtn     = document.getElementById('chooseFieldBtn');
const roleModalMessage   = document.getElementById('roleModalMessage');

const gameOverOverlay    = document.getElementById('gameOverOverlay');
const gameOverIcon       = document.getElementById('gameOverIcon');
const gameOverTitle      = document.getElementById('gameOverTitle');
const gameOverDesc       = document.getElementById('gameOverDesc');
const gameOverBackBtn    = document.getElementById('gameOverBackBtn');

/* ============================================
   3. EVENT LISTENERS
   ============================================ */
startBtn.addEventListener('click', handleCreatePlayer);
playerNameInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') handleCreatePlayer();
});
logoutBtn.addEventListener('click', handleLogout);
createRoomBtn.addEventListener('click', handleCreateRoom);
joinRoomBtn.addEventListener('click', handleJoinRoom);
roomCodeInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') handleJoinRoom();
});
backToLobbyBtn.addEventListener('click', handleBackToLobby);
startGameBtn.addEventListener('click', handleStartGame);
copyRoomCodeBtn.addEventListener('click', handleCopyRoomCode);
chooseSpymasterBtn.addEventListener('click', () => handleChooseRole('spymaster'));
chooseFieldBtn.addEventListener('click', () => handleChooseRole('field-operative'));
gameOverBackBtn.addEventListener('click', () => {
    gameOverOverlay.style.display = 'none';
    stopGamePoller();
    gameState.room   = { code: null, status: null, players: [], cards: [], state: null };
    gameState.myRole = null;
    goToLobby();
});

/* ============================================
   4. INITIALIZATION
   ============================================ */
document.addEventListener('DOMContentLoaded', () => {
    const savedPlayer = localStorage.getItem('player');
    if (savedPlayer) {
        gameState.player = JSON.parse(savedPlayer);
        goToLobby();
    }
});

/* ============================================
   5. PAGE NAVIGATION
   ============================================ */
function showPage(pageElement) {
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    pageElement.classList.add('active');
}

function goToWelcome() {
    stopWaitingPoller();
    stopGamePoller();
    showPage(welcomePage);
}

function goToLobby() {
    stopWaitingPoller();
    stopGamePoller();
    playerNameDisplay.textContent = gameState.player.name;
    loadAvailableRooms();
    showPage(lobbyPage);
}

function goToWaitingRoom() {
    stopGamePoller();
    roomCodeDisplay.textContent = gameState.room.code;
    loadWaitingRoomDetails();
    startWaitingPoller();
    showPage(waitingRoomPage);
}

function goToGame() {
    stopWaitingPoller();
    gameRoomCode.textContent = gameState.room.code;
    loadAndRenderBoard();
    startGamePoller();
    showPage(gamePage);
}

/* ============================================
   6. PLAYER MANAGEMENT
   ============================================ */
async function handleCreatePlayer() {
    const name = playerNameInput.value.trim();
    if (!name || name.length < 2) {
        showMessage(welcomeMessage, 'Name must be at least 2 characters', 'error');
        return;
    }

    startBtn.disabled = true;
    startBtn.classList.add('loading');
    try {
        const res = await fetch(`${API_BASE_URL}/players`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name })
        });
        if (!res.ok) throw new Error('Failed to create player');
        const data = await res.json();
        gameState.player = { id: data.id, name };
        localStorage.setItem('player', JSON.stringify(gameState.player));
        showMessage(welcomeMessage, '✅ Welcome! Redirecting...', 'success');
        setTimeout(() => goToLobby(), 1000);
    } catch (err) {
        showMessage(welcomeMessage, `❌ ${err.message}`, 'error');
    } finally {
        startBtn.disabled = false;
        startBtn.classList.remove('loading');
    }
}

function handleLogout() {
    localStorage.removeItem('player');
    gameState.player = { id: null, name: null };
    gameState.room   = { code: null, status: null, players: [], cards: [], state: null };
    gameState.myRole = null;
    playerNameInput.value = '';
    goToWelcome();
}

/* ============================================
   7. ROOM MANAGEMENT
   ============================================ */
async function handleCreateRoom() {
    createRoomBtn.disabled = true;
    createRoomBtn.classList.add('loading');
    try {
        const res = await fetch(`${API_BASE_URL}/rooms`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
        if (!res.ok) throw new Error('Failed to create room');
        const data = await res.json();
        gameState.room.code = data.code;
        showMessage(createRoomMessage, `✅ Room created! Code: ${data.code}`, 'success');
        setTimeout(() => goToWaitingRoom(), 500);
    } catch (err) {
        showMessage(createRoomMessage, `❌ ${err.message}`, 'error');
    } finally {
        createRoomBtn.disabled = false;
        createRoomBtn.classList.remove('loading');
    }
}

async function handleJoinRoom() {
    const code = roomCodeInput.value.trim().toUpperCase();
    if (!code) { showMessage(joinRoomMessage, 'Please enter room code', 'error'); return; }

    joinRoomBtn.disabled = true;
    joinRoomBtn.classList.add('loading');
    try {
        const res = await fetch(`${API_BASE_URL}/rooms/join`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code, playerId: gameState.player.id })
        });
        if (!res.ok) throw new Error('Room not found or already started');
        gameState.room.code = code;
        showMessage(joinRoomMessage, '✅ Joined room!', 'success');
        setTimeout(() => goToWaitingRoom(), 500);
    } catch (err) {
        showMessage(joinRoomMessage, `❌ ${err.message}`, 'error');
    } finally {
        joinRoomBtn.disabled = false;
        joinRoomBtn.classList.remove('loading');
    }
}

async function loadAvailableRooms() {
    try {
        const res = await fetch(`${API_BASE_URL}/rooms`);
        const rooms = await res.json();
        const waitingRooms = rooms.filter(r => r.status === 'Waiting');
        if (waitingRooms.length === 0) {
            availableRoomsList.innerHTML = '<p class="empty-message">No available rooms. Create one!</p>';
            return;
        }
        availableRoomsList.innerHTML = waitingRooms.map(room => `
            <div class="room-item" onclick="quickJoin('${room.code}')">
                <div class="room-details"><h3>Room ${room.code}</h3><p>${room.players.length} players</p></div>
                <div class="room-players">Join →</div>
            </div>
        `).join('');
    } catch {
        availableRoomsList.innerHTML = '<p class="empty-message">Error loading rooms</p>';
    }
}

function quickJoin(code) {
    roomCodeInput.value = code;
    handleJoinRoom();
}

function handleCopyRoomCode() {
    navigator.clipboard.writeText(gameState.room.code).then(() => {
        const orig = copyRoomCodeBtn.textContent;
        copyRoomCodeBtn.textContent = '✓ Copied!';
        setTimeout(() => { copyRoomCodeBtn.textContent = orig; }, 2000);
    });
}

function handleBackToLobby() {
    if (confirm('Are you sure? You will leave the room.')) {
        gameState.room   = { code: null, status: null, players: [], cards: [], state: null };
        gameState.myRole = null;
        goToLobby();
    }
}

/* ============================================
   8. GAMEPLAY
   ============================================ */

// ── Waiting Room ──────────────────────────────────
async function loadWaitingRoomDetails() {
    try {
        const res = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}`);
        if (!res.ok) return;
        const room = await res.json();

        gameState.room = {
            code: room.code, status: room.status,
            players: room.players || [], cards: room.cards || [], state: room.state || {}
        };
        updateWaitingRoomUI();

        // ← SYNC FIX: Deteksi transisi status dan sinkronisasi peran
        if (room.status === 'Playing' || room.status === 'Finished') {
            const me = room.players.find(p => p.id === gameState.player.id);
            const serverRole = me?.gameRole;
            const phase = room.state?.phase;

            // Jika game sudah berjalan, pastikan kita sinkron role dan masuk ke board
            if (serverRole && serverRole !== 'None') {
                gameState.myRole = serverRole;
                hideRoleModal(); // Tutup jika sempat terbuka (safety)
                if (!gamePoller) goToGame();
            }
        }
    } catch (err) {
        console.error('loadWaitingRoomDetails error:', err);
    }
}

function updateWaitingRoomUI() {
    playersList.innerHTML = gameState.room.players.map(p =>
        `<li class="player-item">${p.name}</li>`
    ).join('');

    const canStart = gameState.room.players.length >= 2;
    startGameBtn.disabled = !canStart;
    if (canStart) {
        minPlayersMsg.style.display = 'none';
        startGameInfo.textContent  = '✅ Ready to start!';
        startGameInfo.style.color  = 'var(--success)';
    } else {
        minPlayersMsg.style.display = 'block';
        startGameInfo.textContent   = `${gameState.room.players.length}/2 players`;
    }
}

async function handleStartGame() {
    startGameBtn.disabled = true;
    startGameBtn.classList.add('loading');
    try {
        const res = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: gameState.player.id }) // Kirim ID penekan tombol
        });
        if (!res.ok) throw new Error(await res.text());
        
        showMessage(startGameInfo, '✅ Game started! You are the Spymaster.', 'success');
        // Role akan disinkronkan oleh poller otomatis
    } catch (err) {
        showMessage(startGameInfo, `❌ ${err.message}`, 'error');
        startGameBtn.disabled = false;
        startGameBtn.classList.remove('loading');
    }
}

// ── Role Modal ────────────────────────────────────
function showRoleModal(room) {
    if (roleModal.style.display !== 'flex') {
        roleModalMessage.className = 'message';
        roleModalMessage.textContent = '';
        roleModal.style.display = 'flex';
    }

    // Hitung spymaster yang sudah ada
    const spymasters = room.players.filter(p => p.gameRole === 'Spymaster');
    const fieldOps   = room.players.filter(p => p.gameRole === 'FieldOperative');
    
    // Update tombol Spymaster (Limit 1 sesuai permintaan eksplisit)
    if (spymasters.length >= 1) {
        chooseSpymasterBtn.disabled = true;
        chooseSpymasterBtn.innerHTML = `
            <span class="role-icon">🕵️</span>
            <span class="role-name">Spymaster (TAKEN)</span>
            <span class="role-desc">Taken by: ${spymasters[0].name}</span>
        `;
    } else {
        chooseSpymasterBtn.disabled = false;
        chooseSpymasterBtn.innerHTML = `
            <span class="role-icon">🕵️</span>
            <span class="role-name">Spymaster</span>
            <span class="role-desc">See all card colors, give clues. (0/1)</span>
        `;
    }

    // Info Field Operative
    chooseFieldBtn.innerHTML = `
        <span class="role-icon">🧑‍💼</span>
        <span class="role-name">Field Operative</span>
        <span class="role-desc">Guess words based on clues. (${fieldOps.length} joined)</span>
    `;
}

function hideRoleModal() {
    roleModal.style.display = 'none';
}

async function handleChooseRole(role) {
    chooseSpymasterBtn.disabled = true;
    chooseFieldBtn.disabled     = true;

    const endpoint = role === 'spymaster'
        ? `${API_BASE_URL}/rooms/${gameState.room.code}/assign-spymaster`
        : `${API_BASE_URL}/rooms/${gameState.room.code}/assign-field-operative`;

    try {
        const res = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: gameState.player.id })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed');
        }
        gameState.myRole = role === 'spymaster' ? 'Spymaster' : 'FieldOperative';
        showMessage(roleModalMessage, `✅ You are now the ${gameState.myRole === 'Spymaster' ? 'Spymaster 🕵️' : 'Field Operative 🧑‍💼'}!`, 'success');
        setTimeout(() => { hideRoleModal(); goToGame(); }, 900);
    } catch (err) {
        showMessage(roleModalMessage, `❌ ${err.message}`, 'error');
        chooseSpymasterBtn.disabled = false;
        chooseFieldBtn.disabled     = false;
    }
}

// ── Game Board ────────────────────────────────────
async function loadAndRenderBoard() {
    try {
        const url = `${API_BASE_URL}/rooms/${gameState.room.code}?playerId=${gameState.player.id}`;
        const res  = await fetch(url);
        if (!res.ok) return;
        const room = await res.json();

        gameState.room = {
            code: room.code, status: room.status,
            players: room.players || [], cards: room.cards || [], state: room.state || {}
        };

        // Sinkronisasi role dari server
        if (!gameState.myRole || gameState.myRole === 'None') {
            const me = room.players.find(p => p.id === gameState.player.id);
            if (me && me.gameRole !== 'None') gameState.myRole = me.gameRole;
        }

        // Deteksi game over
        if (room.status === 'Finished' || room.state?.winner) {
            stopGamePoller();
            renderBoard(room);
            showGameOver(room.state?.winner);
            return;
        }

        renderBoard(room);
    } catch (err) {
        console.error('loadAndRenderBoard error:', err);
    }
}

function renderBoard(room) {
    const isSpymaster = gameState.myRole === 'Spymaster';

    // Header stats
    gameRound.textContent  = `Round: ${room.state?.round || 1}`;
    gameStatus.textContent = `Status: ${room.status}`;
    if (gamePhase) {
        const p = room.state?.phase || '';
        gamePhase.textContent =
            p === 'SpymasterSelection' ? '⏳ Choosing Roles...' :
            p === 'Playing'            ? '🎮 Playing'           :
            p === 'Ended'              ? '🏁 Game Over'         : p;
    }

    // My role badge
    if (myRoleBadge) {
        if (isSpymaster) {
            myRoleBadge.textContent = '🕵️ You are the Spymaster';
            myRoleBadge.className   = 'my-role-badge spymaster';
        } else if (gameState.myRole === 'FieldOperative') {
            myRoleBadge.textContent = '🧑‍💼 You are a Field Operative';
            myRoleBadge.className   = 'my-role-badge field-operative';
        } else {
            myRoleBadge.textContent = '';
            myRoleBadge.className   = 'my-role-badge';
        }
    }

    // Cards
    cardsGrid.innerHTML = '';
    (room.cards || []).forEach(card => {
        const el = document.createElement('button');
        el.className  = 'card';
        el.textContent = card.word;

        if (card.isRevealed) {
            // Sudah direveal → warna penuh
            el.classList.add('revealed', getRoleClass(card.role));
            el.disabled = true;
        } else if (isSpymaster && card.role) {
            // Spymaster → hint warna background
            el.classList.add(getHintClass(card.role));
            el.disabled = true;
        } else {
            // Field Operative → bisa klik
            el.addEventListener('click', () => handleRevealCard(card, el));
        }

        cardsGrid.appendChild(el);
    });

    // Players list
    gamePlayers.innerHTML = (room.players || []).map(p => {
        const roleTag = p.gameRole && p.gameRole !== 'None'
            ? `<span class="player-role-tag">${p.gameRole === 'Spymaster' ? '🕵️' : '🧑‍💼'}</span>`
            : `<span class="player-role-tag">⏳</span>`;
        const isSelf  = p.id === gameState.player.id ? ' (you)' : '';
        return `<li class="player-item">${p.name}${isSelf}${roleTag}</li>`;
    }).join('');
}

async function handleRevealCard(card, el) {
    if (card.isRevealed) return;
    
    // Safety check role
    if (gameState.myRole !== 'FieldOperative') {
        alert("Only Field Operatives can reveal cards. You are: " + (gameState.myRole || "None"));
        return;
    }

    el.disabled = true;
    el.classList.add('loading');
    try {
        const res = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}/cards/${card.id}/reveal`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: gameState.player.id })
        });
        
        if (!res.ok) {
            const errorMsg = await res.text();
            throw new Error(errorMsg || 'Failed to reveal card');
        }

        await loadAndRenderBoard();
    } catch (err) {
        console.error('RevealCard error:', err);
        alert(`❌ Error: ${err.message}`);
        el.disabled = false;
        el.classList.remove('loading');
    }
}

// ── Game Over ─────────────────────────────────────
function showGameOver(winner) {
    const box = gameOverOverlay.querySelector('.modal-box');
    box.classList.remove('winner-red', 'winner-blue', 'winner-assassin');

    if (winner === 'Assassin') {
        box.classList.add('winner-assassin');
        gameOverIcon.textContent  = '💀';
        gameOverTitle.textContent = 'Assassin Revealed!';
        gameOverDesc.textContent  = 'The Assassin card was revealed. Game over!';
    } else if (winner === 'RedTeam') {
        box.classList.add('winner-red');
        gameOverIcon.textContent  = '🔴';
        gameOverTitle.textContent = 'Red Team Wins!';
        gameOverDesc.textContent  = 'All Red Agent cards have been revealed. Red Team is victorious!';
    } else if (winner === 'BlueTeam') {
        box.classList.add('winner-blue');
        gameOverIcon.textContent  = '🔵';
        gameOverTitle.textContent = 'Blue Team Wins!';
        gameOverDesc.textContent  = 'All Blue Agent cards have been revealed. Blue Team is victorious!';
    } else {
        gameOverIcon.textContent  = '🏁';
        gameOverTitle.textContent = 'Game Over!';
        gameOverDesc.textContent  = '';
    }

    gameOverOverlay.style.display = 'flex';
}

/* ============================================
   9. UTILITIES
   ============================================ */

// ── Waitingroom poller ──
function startWaitingPoller() {
    if (waitingPoller) return;
    waitingPoller = setInterval(() => {
        if (gameState.room.code) loadWaitingRoomDetails();
    }, WAITING_POLL_MS);
}
function stopWaitingPoller() {
    if (waitingPoller) { clearInterval(waitingPoller); waitingPoller = null; }
}

// ── Game page poller ──
function startGamePoller() {
    if (gamePoller) return;
    gamePoller = setInterval(() => {
        if (gameState.room.code) loadAndRenderBoard();
    }, GAME_POLL_MS);
}
function stopGamePoller() {
    if (gamePoller) { clearInterval(gamePoller); gamePoller = null; }
}

// ── Card role class helpers ──
function getRoleClass(role) {
    switch (role) {
        case 'RedAgent':  return 'red-agent';
        case 'BlueAgent': return 'blue-agent';
        case 'Bystander': return 'bystander';
        case 'Assassin':  return 'assassin';
        default:          return '';
    }
}
function getHintClass(role) {
    switch (role) {
        case 'RedAgent':  return 'hint-red';
        case 'BlueAgent': return 'hint-blue';
        case 'Bystander': return 'hint-bystander';
        case 'Assassin':  return 'hint-assassin';
        default:          return '';
    }
}

// ── Message helper ──
function showMessage(el, msg, type) {
    el.textContent = msg;
    el.className   = `message show ${type}`;
    if (type === 'success') setTimeout(() => el.classList.remove('show'), 4000);
}
