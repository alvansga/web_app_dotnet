// ===== Configuration =====
const API_BASE_URL = '/api';
const POLLING_INTERVAL = 2000; // Poll every 2 seconds for room updates

// ===== State Management =====
let gameState = {
    player: {
        id: null,
        name: null
    },
    room: {
        code: null,
        status: null,
        players: [],
        cards: [],
        state: null
    }
};

let pollingInterval = null;

// ===== DOM Elements - Welcome =====
const welcomePage = document.getElementById('welcomePage');
const playerNameInput = document.getElementById('playerNameInput');
const startBtn = document.getElementById('startBtn');
const welcomeMessage = document.getElementById('welcomeMessage');

// ===== DOM Elements - Lobby =====
const lobbyPage = document.getElementById('lobbyPage');
const playerNameDisplay = document.getElementById('playerNameDisplay');
const logoutBtn = document.getElementById('logoutBtn');
const createRoomBtn = document.getElementById('createRoomBtn');
const createRoomMessage = document.getElementById('createRoomMessage');
const roomCodeInput = document.getElementById('roomCodeInput');
const joinRoomBtn = document.getElementById('joinRoomBtn');
const joinRoomMessage = document.getElementById('joinRoomMessage');
const availableRoomsList = document.getElementById('availableRoomsList');

// ===== DOM Elements - Waiting Room =====
const waitingRoomPage = document.getElementById('waitingRoomPage');
const roomCodeDisplay = document.getElementById('roomCodeDisplay');
const backToLobbyBtn = document.getElementById('backToLobbyBtn');
const playersList = document.getElementById('playersList');
const minPlayersMsg = document.getElementById('minPlayersMsg');
const startGameBtn = document.getElementById('startGameBtn');
const startGameInfo = document.getElementById('startGameInfo');
const copyRoomCodeBtn = document.getElementById('copyRoomCodeBtn');

// ===== DOM Elements - Game =====
const gamePage = document.getElementById('gamePage');
const gameRoomCode = document.getElementById('gameRoomCode');
const gameRound = document.getElementById('gameRound');
const gameStatus = document.getElementById('gameStatus');
const cardsGrid = document.getElementById('cardsGrid');
const gamePlayers = document.getElementById('gamePlayers');

// ===== Event Listeners =====
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

// ===== Initialize =====
document.addEventListener('DOMContentLoaded', () => {
    const savedPlayer = localStorage.getItem('player');
    if (savedPlayer) {
        gameState.player = JSON.parse(savedPlayer);
        goToLobby();
    }
});

// ===== Page Navigation =====
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

// ===== Create Player =====
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

// ===== Logout =====
function handleLogout() {
    localStorage.removeItem('player');
    gameState.player = { id: null, name: null };
    gameState.room = { code: null, status: null, players: [], cards: [], state: null };
    goToWelcome();
    playerNameInput.value = '';
}

// ===== Create Room =====
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

// ===== Join Room =====
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

// ===== Load Available Rooms =====
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

// ===== Quick Join Room =====
function quickJoinRoom(code) {
    roomCodeInput.value = code;
    handleJoinRoom();
}

// ===== Load Room Details =====
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

        // If game started, go to game page
        if (room.status === 'Playing') {
            goToGame();
        }

    } catch (error) {
        console.error('Error loading room details:', error);
    }
}

// ===== Update Waiting Room UI =====
function updateWaitingRoomUI() {
    // Update players list
    playersList.innerHTML = gameState.room.players.map(p => 
        `<li class="player-item">${p.name}</li>`
    ).join('');

    // Enable start button if enough players
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

// ===== Start Game =====
async function handleStartGame() {
    startGameBtn.disabled = true;
    startGameBtn.classList.add('loading');

    try {
        // For now, generate 25 random words
        const words = generateRandomWords(25);
        
        const response = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ words })
        });

        if (!response.ok) throw new Error('Failed to start game');

        showMessage(startGameInfo, '✅ Game started!', 'success');
        await loadRoomDetails();
        setTimeout(() => goToGame(), 1000);

    } catch (error) {
        console.error('Error starting game:', error);
        showMessage(startGameInfo, `❌ ${error.message}`, 'error');
        startGameBtn.disabled = false;
        startGameBtn.classList.remove('loading');
    }
}

// ===== Generate Random Words =====
function generateRandomWords(count) {
    const words = [
        'APPLE', 'BANANA', 'CASTLE', 'DIAMOND', 'ELEPHANT',
        'FOREST', 'GUITAR', 'HOUSE', 'ISLAND', 'JUNGLE',
        'KEYBOARD', 'LION', 'MOUNTAIN', 'NOTEBOOK', 'OCEAN',
        'PIANO', 'QUEEN', 'RIVER', 'SCHOOL', 'TIGER',
        'UMBRELLA', 'VIOLIN', 'WINDOW', 'XENOPHOBE', 'ZEBRA'
    ];
    return words.slice(0, count);
}

// ===== Load Game Board =====
async function loadGameBoard() {
    try {
        const response = await fetch(`${API_BASE_URL}/rooms/${gameState.room.code}`);
        if (!response.ok) throw new Error('Failed to load game');

        const room = await response.json();
        gameState.room = {
            code: room.code,
            status: room.status,
            players: room.players || [],
            cards: room.cards || [],
            state: room.state || {}
        };

        // Render cards
        cardsGrid.innerHTML = '';
        gameState.room.cards.forEach(card => {
            const cardEl = document.createElement('button');
            cardEl.className = 'card';
            cardEl.textContent = card.word;
            
            if (card.isRevealed) {
                cardEl.classList.add('revealed');
                cardEl.classList.add(getRoleClass(card.role));
            }
            
            cardEl.addEventListener('click', () => selectCard(card));
            cardsGrid.appendChild(cardEl);
        });

        // Update players
        gamePlayers.innerHTML = gameState.room.players.map(p =>
            `<li class="player-item">${p.name}</li>`
        ).join('');

        // Update stats
        gameRound.textContent = `Round: ${gameState.room.state.round || 1}`;
        gameStatus.textContent = `Status: ${gameState.room.status}`;

    } catch (error) {
        console.error('Error loading game board:', error);
    }
}

// ===== Get Role Class =====
function getRoleClass(role) {
    switch (role) {
        case 'RedAgent': return 'red-agent';
        case 'BlueAgent': return 'blue-agent';
        case 'Bystander': return 'bystander';
        case 'Assassin': return 'assassin';
        default: return '';
    }
}

// ===== Select Card =====
function selectCard(card) {
    if (card.isRevealed) return;
    // TODO: Implement card reveal logic
    console.log('Card selected:', card);
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
        gameState.room = { code: null, status: null, players: [], cards: [], state: null };
        goToLobby();
    }
}

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
