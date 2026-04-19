/**
 * CODENAME GAME - JavaScript Controller
 * ======================================
 * Manages: Game logic, API calls, UI updates
 * 
 * Organized Sections:
 * 1. Config & State      - Constants and global variables
 * 2. DOM Elements        - UI element references
 * 3. Event Listeners     - User interaction handlers
 * 4. Initialization      - App startup
 * 5. Page Navigation     - Switch between pages
 * 6. Player Management   - Create player, logout
 * 7. Room Management     - Create/join rooms
 * 8. Gameplay            - Start game, role selection, board
 * 9. Utilities           - Helper functions
 */

/* ============================================
   1. CONFIG & STATE - Constants and variables
   ============================================ */
const API_BASE_URL = '/api';
const POLLING_INTERVAL = 2000;

// Global game state object
let gameState = {
    player: { id: null, name: null },
    room: { code: null, status: null, players: [], cards: [], state: null },
    myRole: null  // 'Spymaster' | 'FieldOperative' | null
};

let pollingInterval = null;

/* ============================================
   2. DOM ELEMENTS - UI element references
   ============================================ */
const welcomePage        = document.getElementById('welcomePage');
const playerNameInput    = document.getElementById('playerNameInput');
const startBtn           = document.getElementById('startBtn');
const welcomeMessage     = document.getElementById('welcomeMessage');

// Lobby
const lobbyPage          = document.getElementById('lobbyPage');
const playerNameDisplay  = document.getElementById('playerNameDisplay');
const logoutBtn          = document.getElementById('logoutBtn');
const createRoomBtn      = document.getElementById('createRoomBtn');
const createRoomMessage  = document.getElementById('createRoomMessage');
const roomCodeInput      = document.getElementById('roomCodeInput');
const joinRoomBtn        = document.getElementById('joinRoomBtn');
const joinRoomMessage    = document.getElementById('joinRoomMessage');
const availableRoomsList = document.getElementById('availableRoomsList');

// Waiting Room
const waitingRoomPage    = document.getElementById('waitingRoomPage');
const roomCodeDisplay    = document.getElementById('roomCodeDisplay');
const backToLobbyBtn     = document.getElementById('backToLobbyBtn');
const playersList        = document.getElementById('playersList');
const minPlayersMsg      = document.getElementById('minPlayersMsg');
const startGameBtn       = document.getElementById('startGameBtn');
const startGameInfo      = document.getElementById('startGameInfo');
const copyRoomCodeBtn    = document.getElementById('copyRoomCodeBtn');

// Game
const gamePage           = document.getElementById('gamePage');
const gameRoomCode       = document.getElementById('gameRoomCode');
const gameRound          = document.getElementById('gameRound');
const gameStatus         = document.getElementById('gameStatus');
const gamePhase          = document.getElementById('gamePhase');
const cardsGrid          = document.getElementById('cardsGrid');
const gamePlayers        = document.getElementById('gamePlayers');
const myRoleBadge        = document.getElementById('myRoleBadge');

// Role Selection Modal
const roleModal          = document.getElementById('roleModal');
const chooseSpymasterBtn = document.getElementById('chooseSpymasterBtn');
const chooseFieldBtn     = document.getElementById('chooseFieldBtn');
const roleModalMessage   = document.getElementById('roleModalMessage');

/* ============================================
   3. EVENT LISTENERS - User interactions
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

// Role Modal buttons
chooseSpymasterBtn.addEventListener('click', () => handleChooseRole('spymaster'));
chooseFieldBtn.addEventListener('click', () => handleChooseRole('field-operative'));

/* ============================================
   4. INITIALIZATION - App startup
   ============================================ */
document.addEventListener('DOMContentLoaded', () => {
    const savedPlayer = localStorage.getItem('player');
    if (savedPlayer) {
        gameState.player = JSON.parse(savedPlayer);
        goToLobby();
    }
});

/* ============================================
   5. PAGE NAVIGATION - Switch between pages
   ============================================ */
function showPage(pageElement) {
    document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
    pageElement.classList.add('active');
}

function goToWelcome() {
    stopPolling();
    showPage(welcomePage);
}

function goToLobby() {
    stopPolling();
    playerNameDisplay.textContent = gameState.player.name;
    loadAvailableRooms();
    showPage(lobbyPage);
}

function goToWaitingRoom() {
    roomCodeDisplay.textContent = gameState.room.code;
    loadRoomDetails();
    startPolling();
    showPage(waitingRoomPage);
}

function goToGame() {
    stopPolling();
    gameRoomCode.textContent = gameState.room.code;
    loadGameBoard();
    showPage(gamePage);
}

/* ============================================
   6. PLAYER MANAGEMENT - Create & manage players
   ============================================ */
async function handleCreatePlayer() {
    const name = playerNameInput.value.trim();

    if (!name) {
        showMessage(welcomeMessage, 'Please enter your name', 'error');
        return;
    }

    if (name.length < 2) {
        showMessage(welcomeMessage, 'Name must be at least 2 characters', 'error');
        return;
    }

    startBtn.disabled = true;
    startBtn.classList.add('loading');

    try {
        const response = await fetch(`${API_BASE_URL}/players`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name })
        });

        if (!response.ok) throw new Error('Failed to create player');

        const data = await response.json();
        
        gameState.player.id = data.id;
        gameState.player.name = name;
        localStorage.setItem('player', JSON.stringify(gameState.player));

        showMessage(welcomeMessage, '✅ Welcome! Redirecting...', 'success');
        setTimeout(() => goToLobby(), 1000);

    } catch (error) {
        console.error('Error creating player:', error);
        showMessage(welcomeMessage, `❌ ${error.message}`, 'error');
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
   7. ROOM MANAGEMENT - Create & join rooms
   ============================================ */
async function handleCreateRoom() {
    createRoomBtn.disabled = true;
    createRoomBtn.classList.add('loading');

    try {
        const response = await fetch(`${API_BASE_URL}/rooms`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        if (!response.ok) throw new Error('Failed to create room');

        const data = await response.json();
        gameState.room.code = data.code;

        showMessage(createRoomMessage, `✅ Room created! Code: ${data.code}`, 'success');
        await loadRoomDetails();
        setTimeout(() => goToWaitingRoom(), 500);

    } catch (error) {
        console.error('Error creating room:', error);
        showMessage(createRoomMessage, `❌ ${error.message}`, 'error');
    } finally {
        createRoomBtn.disabled = false;
        createRoomBtn.classList.remove('loading');
    }
}

async function handleJoinRoom() {
    const code = roomCodeInput.value.trim().toUpperCase();

    if (!code) {
        showMessage(joinRoomMessage, 'Please enter room code', 'error');
        return;
    }

    joinRoomBtn.disabled = true;
    joinRoomBtn.classList.add('loading');

    try {
        const response = await fetch(`${API_BASE_URL}/rooms/join`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                code: code,
                playerId: gameState.player.id
            })
        });

        if (!response.ok) {
            if (response.status === 400) throw new Error('Room not found or already started');
            throw new Error('Failed to join room');
        }

        gameState.room.code = code;
        showMessage(joinRoomMessage, '✅ Joined room!', 'success');
        setTimeout(() => goToWaitingRoom(), 500);

    } catch (error) {
        console.error('Error joining room:', error);
        showMessage(joinRoomMessage, `❌ ${error.message}`, 'error');
    } finally {
        joinRoomBtn.disabled = false;
        joinRoomBtn.classList.remove('loading');
    }
}

async function loadAvailableRooms() {
    try {
        const response = await fetch(`${API_BASE_URL}/rooms`);
        if (!response.ok) throw new Error('Failed to load rooms');

        const rooms = await response.json();
        const waitingRooms = rooms.filter(r => r.status === 'Waiting');

        if (waitingRooms.length === 0) {
            availableRoomsList.innerHTML = '<p class="empty-message">No available rooms. Create one to get started!</p>';
            return;
        }

        availableRoomsList.innerHTML = waitingRooms.map(room => `
            <div class="room-item" onclick="quickJoinRoom('${room.code}')">
                <div class="room-details">
                    <h3>Room ${room.code}</h3>
                    <p>Status: ${room.status}</p>
                </div>
                <div class="room-players">${room.players.length} players</div>
            </div>
        `).join('');

    } catch (error) {
        console.error('Error loading rooms:', error);
        availableRoomsList.innerHTML = '<p class="empty-message">Error loading rooms</p>';
    }
}

function quickJoinRoom(code) {
    roomCodeInput.value = code;
    handleJoinRoom();
}

async function loadRoomDetails() {
    try {
        const response = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}`);
        if (!response.ok) throw new Error('Room not found');

        const room = await response.json();
        gameState.room = {
            code: room.code,
            status: room.status,
            players: room.players || [],
            cards: room.cards || [],
            state: room.state || {}
        };

        updateWaitingRoomUI();

        // Jika game sudah dimulai (status Playing)
        if (room.status === 'Playing') {
            // Cek apakah player ini sudah punya role
            const myPlayer = room.players.find(p => p.id === gameState.player.id);
            if (myPlayer && myPlayer.gameRole !== 'None') {
                // Sudah punya role, langsung ke game
                gameState.myRole = myPlayer.gameRole;
                goToGame();
            } else {
                // Belum punya role, tampilkan modal
                stopPolling();
                showRoleModal();
            }
        }

    } catch (error) {
        console.error('Error loading room details:', error);
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
        startGameInfo.textContent = '✅ Ready to start!';
        startGameInfo.style.color = 'var(--success)';
    } else {
        minPlayersMsg.style.display = 'block';
        startGameInfo.textContent = `${gameState.room.players.length}/2 players`;
    }
}

/* ============================================
   8. GAMEPLAY - Start game, role selection, board
   ============================================ */

// ===== Start Game =====
async function handleStartGame() {
    startGameBtn.disabled = true;
    startGameBtn.classList.add('loading');

    try {
        const response = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });

        if (!response.ok) throw new Error('Failed to start game');

        showMessage(startGameInfo, '✅ Game started!', 'success');
        // Tampilkan modal pemilihan peran
        setTimeout(() => showRoleModal(), 800);

    } catch (error) {
        console.error('Error starting game:', error);
        showMessage(startGameInfo, `❌ ${error.message}`, 'error');
        startGameBtn.disabled = false;
        startGameBtn.classList.remove('loading');
    }
}

// ===== Show Role Modal =====
function showRoleModal() {
    roleModalMessage.className = 'message';
    roleModalMessage.textContent = '';
    chooseSpymasterBtn.disabled = false;
    chooseFieldBtn.disabled = false;
    roleModal.style.display = 'flex';
}

function hideRoleModal() {
    roleModal.style.display = 'none';
}

// ===== Choose Role =====
async function handleChooseRole(role) {
    chooseSpymasterBtn.disabled = true;
    chooseFieldBtn.disabled = true;

    const endpoint = role === 'spymaster'
        ? `${API_BASE_URL}/rooms/${gameState.room.code}/assign-spymaster`
        : `${API_BASE_URL}/rooms/${gameState.room.code}/assign-field-operative`;

    try {
        const response = await fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: gameState.player.id })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || 'Failed to assign role');
        }

        gameState.myRole = role === 'spymaster' ? 'Spymaster' : 'FieldOperative';
        showMessage(roleModalMessage, `✅ You are now the ${gameState.myRole === 'Spymaster' ? 'Spymaster 🕵️' : 'Field Operative 🧑‍💼'}!`, 'success');

        setTimeout(() => {
            hideRoleModal();
            goToGame();
        }, 900);

    } catch (error) {
        console.error('Error choosing role:', error);
        showMessage(roleModalMessage, `❌ ${error.message}`, 'error');
        chooseSpymasterBtn.disabled = false;
        chooseFieldBtn.disabled = false;
    }
}

// ===== Load Game Board =====
async function loadGameBoard() {
    try {
        // Kirim playerId agar server tahu apakah kita spymaster (sehingga role card dikirim)
        const url = `${API_BASE_URL}/rooms/${gameState.room.code}?playerId=${gameState.player.id}`;
        const response = await fetch(url);
        if (!response.ok) throw new Error('Failed to load game');

        const room = await response.json();
        gameState.room = {
            code: room.code,
            status: room.status,
            players: room.players || [],
            cards: room.cards || [],
            state: room.state || {}
        };

        // Sinkronisasi role dari server jika belum di-set
        if (!gameState.myRole) {
            const myPlayer = room.players.find(p => p.id === gameState.player.id);
            if (myPlayer) gameState.myRole = myPlayer.gameRole;
        }

        renderBoard(room);

    } catch (error) {
        console.error('Error loading game board:', error);
    }
}

// ===== Render Board =====
function renderBoard(room) {
    const isSpymaster = gameState.myRole === 'Spymaster';

    // Update header stats
    gameRound.textContent  = `Round: ${room.state?.round || 1}`;
    gameStatus.textContent = `Status: ${room.status}`;
    if (gamePhase) {
        const phase = room.state?.phase || '';
        gamePhase.textContent = phase === 'SpymasterSelection' ? '⏳ Choosing Roles...' 
                              : phase === 'Playing' ? '🎮 Playing' : phase;
    }

    // My role badge
    if (myRoleBadge) {
        if (isSpymaster) {
            myRoleBadge.textContent = '🕵️ You are the Spymaster';
            myRoleBadge.className = 'my-role-badge spymaster';
        } else if (gameState.myRole === 'FieldOperative') {
            myRoleBadge.textContent = '🧑‍💼 You are a Field Operative';
            myRoleBadge.className = 'my-role-badge field-operative';
        } else {
            myRoleBadge.textContent = '';
            myRoleBadge.className = 'my-role-badge';
        }
    }

    // Render cards
    cardsGrid.innerHTML = '';
    gameState.room.cards.forEach(card => {
        const cardEl = document.createElement('button');
        cardEl.className = 'card';
        cardEl.textContent = card.word;

        if (card.isRevealed) {
            // Kartu sudah direveal → tampilkan warna penuh
            cardEl.classList.add('revealed');
            cardEl.classList.add(getRoleClass(card.role));
            cardEl.disabled = true;
        } else if (isSpymaster && card.role) {
            // Spymaster: tampilkan hint warna tapi bukan full reveal
            cardEl.classList.add(getHintClass(card.role));
            cardEl.disabled = true; // spymaster tidak bisa reveal kartu
        } else {
            // Field operative: kartu polos, bisa diklik
            cardEl.addEventListener('click', () => handleRevealCard(card, cardEl));
        }

        cardsGrid.appendChild(cardEl);
    });

    // Players list dengan role tag
    gamePlayers.innerHTML = gameState.room.players.map(p => {
        const roleTag = p.gameRole && p.gameRole !== 'None'
            ? `<span class="player-role-tag">${p.gameRole === 'Spymaster' ? '🕵️' : '🧑‍💼'}</span>`
            : '<span class="player-role-tag">⏳</span>';
        const isSelf = p.id === gameState.player.id ? ' (you)' : '';
        return `<li class="player-item">${p.name}${isSelf}${roleTag}</li>`;
    }).join('');
}

// ===== Reveal Card (Field Operative only) =====
async function handleRevealCard(card, cardEl) {
    if (card.isRevealed) return;
    if (gameState.myRole !== 'FieldOperative') return;

    cardEl.disabled = true;
    cardEl.classList.add('loading');

    try {
        const response = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}/cards/${card.id}/reveal`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId: gameState.player.id })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(text || 'Failed to reveal card');
        }

        // Refresh board setelah reveal
        await loadGameBoard();

    } catch (error) {
        console.error('Error revealing card:', error);
        cardEl.disabled = false;
        cardEl.classList.remove('loading');
    }
}

// ===== Get Role Class (for revealed cards) =====
function getRoleClass(role) {
    switch (role) {
        case 'RedAgent':  return 'red-agent';
        case 'BlueAgent': return 'blue-agent';
        case 'Bystander': return 'bystander';
        case 'Assassin':  return 'assassin';
        default: return '';
    }
}

// ===== Get Hint Class (for spymaster unrevealed cards) =====
function getHintClass(role) {
    switch (role) {
        case 'RedAgent':  return 'hint-red';
        case 'BlueAgent': return 'hint-blue';
        case 'Bystander': return 'hint-bystander';
        case 'Assassin':  return 'hint-assassin';
        default: return '';
    }
}

// ===== Copy Room Code =====
function handleCopyRoomCode() {
    navigator.clipboard.writeText(gameState.room.code).then(() => {
        const originalText = copyRoomCodeBtn.textContent;
        copyRoomCodeBtn.textContent = '✓ Copied!';
        setTimeout(() => {
            copyRoomCodeBtn.textContent = originalText;
        }, 2000);
    });
}

// ===== Back to Lobby =====
function handleBackToLobby() {
    if (confirm('Are you sure? You will leave the room.')) {
        gameState.room   = { code: null, status: null, players: [], cards: [], state: null };
        gameState.myRole = null;
        goToLobby();
    }
}

/* ============================================
   9. UTILITIES - Helper functions
   ============================================ */

// ===== Polling =====
function startPolling() {
    if (pollingInterval) return;
    pollingInterval = setInterval(() => {
        if (gameState.room.code) {
            loadRoomDetails();
        }
    }, POLLING_INTERVAL);
}

function stopPolling() {
    if (pollingInterval) {
        clearInterval(pollingInterval);
        pollingInterval = null;
    }
}

// ===== Show Message =====
function showMessage(element, message, type) {
    element.textContent = message;
    element.className = `message show ${type}`;

    if (type === 'success') {
        setTimeout(() => {
            element.classList.remove('show');
        }, 4000);
    }
}
