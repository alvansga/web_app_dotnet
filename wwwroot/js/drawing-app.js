"use strict";

const canvas = document.getElementById('drawing-canvas');
const wrapper = document.getElementById('canvas-wrapper');
const viewport = document.getElementById('viewport');
const ctx = canvas.getContext('2d');
const colorPicker = document.getElementById('color-picker');
const brushSizeRange = document.getElementById('brush-size');
const sizeValueSpan = document.getElementById('size-value');
const clearBtn = document.getElementById('clear-btn');
const saveBtn = document.getElementById('save-btn');
const connectionStatus = document.getElementById('connection-status');
const connectionText = document.getElementById('connection-text');

// --- LOBBY ELEMENTS ---
const lobbyScreen = document.getElementById('lobby-screen');
const playerNameLobby = document.getElementById('player-name-lobby');
const roomCodeInput = document.getElementById('room-code-input');
const joinRoomBtn = document.getElementById('join-room-btn');
const createRoomBtn = document.getElementById('create-room-btn');
const currentRoomCodeSpan = document.getElementById('current-room-code');
const roomInfoBadge = document.querySelector('.room-info-badge');

// --- APP STATE ---
let drawing = false;
let lastX = 0;
let lastY = 0;
let isPanning = false;
let spacePressed = false;
let currentRoomCode = null;

// Game State
let isGameActive = false;
let isDrawer = false;
let gameTimerInterval = null;

// Viewport Transform
let transform = {
    x: 0,
    y: 0,
    scale: 0.8 
};

// Multi-touch tracking
let initialPinchDistance = null;
let initialPinchScale = 1;
let lastMidpoint = { x: 0, y: 0 };
let lastTouchPos = { x: 0, y: 0 };
let currentStrokeId = null;
let strokeHistory = []; 

// DOM Elements
const playBtn = document.getElementById('play-btn');
const gameOverlay = document.getElementById('game-overlay');
const gameTimerDisplay = document.getElementById('game-timer');
const wordDisplay = document.getElementById('word-display');
const targetWordSpan = document.getElementById('target-word');
const drawerInfo = document.getElementById('current-drawer');
const guessPanel = document.getElementById('guess-panel');
const guessInput = document.getElementById('guess-input');
const gameMessages = document.getElementById('game-messages');
const sendGuessBtn = document.getElementById('send-guess-btn');

// World Config
const WORLD_SIZE = 2500;
canvas.width = WORLD_SIZE;
canvas.height = WORLD_SIZE;

// --- INITIALIZE ---
function init() {
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    // Set default name
    playerNameLobby.value = localStorage.getItem('scribble_name') || "Artist" + Math.floor(Math.random() * 1000);

    // Center initially
    const vw = viewport.clientWidth;
    const vh = viewport.clientHeight;
    transform.x = (vw - WORLD_SIZE * transform.scale) / 2;
    transform.y = (vh - WORLD_SIZE * transform.scale) / 2;
    applyTransform();
}

function applyTransform() {
    wrapper.style.transform = `translate(${transform.x}px, ${transform.y}px) scale(${transform.scale})`;
}

// --- SIGNALR SETUP ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/drawingHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveDraw", (data) => {
    strokeHistory.push(data);
    drawLine(data);
});

connection.on("CanvasCleared", () => {
    strokeHistory = [];
    clearLocal();
    if (bgLayer) {
        bgLayer.src = "";
        bgLayer.style.display = 'none';
        bgOptions.style.display = 'none';
    }
});

connection.on("ReceiveBackground", (base64) => {
    updateBackgroundLocal(base64);
});

connection.on("LoadHistory", (history) => {
    strokeHistory = Array.isArray(history) ? history : [];
    redrawCanvas();
});

connection.on("StrokeUndone", (strokeId) => {
    strokeHistory = strokeHistory.filter(s => {
        const sid = s.strokeId || s.StrokeId;
        return sid !== strokeId;
    });
    redrawCanvas();
});

connection.on("GameStarted", (data) => {
    isGameActive = true;
    isDrawer = (connection.connectionId === data.drawerId);
    strokeHistory = [];
    clearLocal();
    
    gameOverlay.style.display = 'block';
    guessPanel.style.display = 'block';
    playBtn.style.display = 'none';
    wordDisplay.style.display = isDrawer ? 'block' : 'none';
    drawerInfo.textContent = isDrawer ? "YOU are drawing!" : `${data.drawerName} is drawing...`;
    
    const startMsg = document.createElement('div');
    startMsg.className = 'msg system';
    startMsg.textContent = `Game started! ${data.drawerName} is drawing.`;
    gameMessages.prepend(startMsg);
    
    startLocalTimer(new Date(data.endTime));
    
    if (isDrawer) setTool('pencil');
    else {
        setTool('move');
        guessInput.focus();
    }
});

connection.on("ReceiveWord", (word) => {
    targetWordSpan.textContent = word;
});

connection.on("ReceiveMessage", (sender, message, isCorrect) => {
    const msgDiv = document.createElement('div');
    msgDiv.className = isCorrect ? 'msg correct' : 'msg';
    msgDiv.innerHTML = `<span class="sender">${sender}:</span> ${message}`;
    gameMessages.prepend(msgDiv);
});

connection.on("GameEnded", (data) => {
    isGameActive = false;
    isDrawer = false;
    clearInterval(gameTimerInterval);
    gameOverlay.style.display = 'none';
    playBtn.style.display = 'block';
    
    const endDiv = document.createElement('div');
    if (data.winnerName) {
        endDiv.className = 'msg correct';
        endDiv.innerHTML = `<strong>${data.winnerName}</strong> guessed the word: <strong>${data.word}</strong>!`;
    } else {
        endDiv.className = 'msg system';
        endDiv.innerHTML = `Time's up! The word was: <strong>${data.word}</strong>`;
    }
    gameMessages.prepend(endDiv);
    
    setTimeout(() => {
        if (!isGameActive) guessPanel.style.display = 'none';
    }, 10000);
});

function startLocalTimer(endTime) {
    if (gameTimerInterval) clearInterval(gameTimerInterval);
    function update() {
        const now = new Date();
        const diff = Math.max(0, Math.floor((endTime - now) / 1000));
        const mins = Math.floor(diff / 60);
        const secs = diff % 60;
        gameTimerDisplay.textContent = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        if (diff <= 0) clearInterval(gameTimerInterval);
    }
    update();
    gameTimerInterval = setInterval(update, 1000);
}

// --- LOBBY ACTIONS ---
createRoomBtn.onclick = async () => {
    const name = playerNameLobby.value.trim() || "Artist";
    localStorage.setItem('scribble_name', name);
    
    try {
        const code = await connection.invoke("CreateRoom");
        joinRoom(code, name);
    } catch (err) {
        console.error("Create Room failed:", err);
        alert("Failed to create room. Please try again.");
    }
};

joinRoomBtn.onclick = () => {
    const code = roomCodeInput.value.trim().toUpperCase();
    const name = playerNameLobby.value.trim() || "Artist";
    if (!code) { alert("Please enter a room code."); return; }
    localStorage.setItem('scribble_name', name);
    joinRoom(code, name);
};

async function joinRoom(code, name) {
    try {
        const success = await connection.invoke("JoinRoom", code, name);
        if (success) {
            currentRoomCode = code;
            currentRoomCodeSpan.textContent = code;
            enterGameUI();
        } else {
            alert("Room not found or invalid!");
        }
    } catch (err) {
        console.error("Join Room failed:", err);
    }
}

function enterGameUI() {
    lobbyScreen.style.display = 'none';
    roomInfoBadge.style.display = 'flex';
    playBtn.style.display = 'block';
    
    // Trigger resize to fix canvas centering
    window.dispatchEvent(new Event('resize'));
}

// --- GAME ACTIONS ---
playBtn.onclick = () => {
    const name = playerNameLobby.value.trim() || "Artist";
    connection.invoke("StartGame", name).catch(err => console.error(err));
};

function sendGuess() {
    const guess = guessInput.value.trim();
    if (!guess || !isGameActive || isDrawer) return;
    const name = playerNameLobby.value.trim() || "Artist";
    connection.invoke("MakeGuess", guess, name).catch(err => console.error(err));
    guessInput.value = "";
}

guessInput.onkeydown = (e) => { if (e.key === "Enter") sendGuess(); };
sendGuessBtn.onclick = sendGuess;

connection.start()
    .then(() => {
        updateStatus('online', 'Connected');
        // If room code in URL, auto-fill it?
        const urlParams = new URLSearchParams(window.location.search);
        const urlRoom = urlParams.get('room');
        if (urlRoom) roomCodeInput.value = urlRoom;
    })
    .catch(e => updateStatus('offline', 'Error'));

function updateStatus(status, text) {
    if (!connectionStatus) return;
    connectionStatus.className = `status-indicator ${status}`;
    connectionText.textContent = text;
}

// --- INTERACTION & DRAWING (Keep logic same as before) ---
function getCoordinates(e) {
    const rect = canvas.getBoundingClientRect();
    let clientX, clientY;
    if (e.touches && e.touches.length > 0) {
        clientX = e.touches[0].clientX;
        clientY = e.touches[0].clientY;
    } else {
        clientX = e.clientX;
        clientY = e.clientY;
    }
    const x = (clientX - rect.left) * (canvas.width / rect.width);
    const y = (clientY - rect.top) * (canvas.height / rect.height);
    return [x, y];
}

let currentTool = 'pencil';
const pencilBtn = document.getElementById('pencil-tool');
const eraserBtn = document.getElementById('eraser-tool');
const moveBtn = document.getElementById('move-tool');

function setTool(tool) {
    currentTool = tool;
    pencilBtn.classList.toggle('active', tool === 'pencil');
    eraserBtn.classList.toggle('active', tool === 'eraser');
    moveBtn.classList.toggle('active', tool === 'move');
    if (tool === 'move') viewport.style.cursor = 'grab';
    else if (tool === 'eraser') viewport.style.cursor = 'cell';
    else viewport.style.cursor = 'crosshair';
}

pencilBtn.onclick = () => setTool('pencil');
eraserBtn.onclick = () => setTool('eraser');
moveBtn.onclick = () => setTool('move');

function startInteraction(e) {
    const isTouch = e.touches && e.touches.length > 0;
    const touchCount = isTouch ? e.touches.length : 1;
    if (touchCount === 2) {
        isPanning = true; drawing = false;
        initialPinchDistance = Math.hypot(e.touches[0].clientX - e.touches[1].clientX, e.touches[0].clientY - e.touches[1].clientY);
        initialPinchScale = transform.scale;
        return;
    }
    if (currentTool === 'move' || e.button === 1 || spacePressed || (isGameActive && !isDrawer)) {
        isPanning = true; drawing = false;
        if (isTouch) lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
    } else {
        drawing = true; isPanning = false;
        currentStrokeId = Date.now().toString() + Math.random().toString(36).substr(2, 9);
        const [x, y] = getCoordinates(e);
        lastX = x; lastY = y;
    }
}

function handleMove(e) {
    const isTouch = e.touches && e.touches.length > 0;
    if (isTouch && e.touches.length === 2) {
        handlePinchOnly(e); return;
    }
    if (isPanning) {
        let dx, dy;
        if (isTouch) {
            dx = e.touches[0].clientX - lastTouchPos.x;
            dy = e.touches[0].clientY - lastTouchPos.y;
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        } else {
            dx = e.movementX; dy = e.movementY;
        }
        transform.x += dx; transform.y += dy;
        applyTransform();
        return;
    }
    if (drawing) {
        const [x, y] = getCoordinates(e);
        const isEraser = (currentTool === 'eraser');
        const drawData = {
            lastX, lastY, x, y,
            color: isEraser ? '#000000' : colorPicker.value,
            size: parseInt(brushSizeRange.value),
            isEraser: isEraser,
            strokeId: currentStrokeId
        };
        strokeHistory.push(drawData);
        drawLine(drawData);
        if (connection.state === "Connected") connection.invoke("DrawLine", drawData);
        [lastX, lastY] = [x, y];
    }
}

function zoomAt(clientX, clientY, newScale) {
    const oldScale = transform.scale;
    const mouseX = clientX - transform.x;
    const mouseY = clientY - transform.y;
    transform.x = clientX - (mouseX / oldScale) * newScale;
    transform.y = clientY - (mouseY / oldScale) * newScale;
    transform.scale = newScale;
}

function handlePinchOnly(e) {
    const t1 = e.touches[0], t2 = e.touches[1];
    const midX = (t1.clientX + t2.clientX) / 2, midY = (t1.clientY + t2.clientY) / 2;
    const dist = Math.hypot(t1.clientX - t2.clientX, t1.clientY - t2.clientY);
    if (initialPinchDistance === null) {
        initialPinchDistance = dist; initialPinchScale = transform.scale;
    } else {
        const factor = dist / initialPinchDistance;
        const newScale = Math.min(Math.max(initialPinchScale * factor, 0.05), 10);
        zoomAt(midX, midY, newScale);
        applyTransform();
    }
}

window.addEventListener('wheel', (e) => {
    if (e.target.closest('.guess-panel') || e.target.closest('.side-panel') || e.target.closest('.app-header')) return;
    if (e.target.closest('#viewport')) {
        e.preventDefault();
        const delta = e.deltaY > 0 ? 0.95 : 1.05;
        zoomAt(e.clientX, e.clientY, transform.scale * delta);
        applyTransform();
    }
}, { passive: false });

canvas.addEventListener('mousedown', startInteraction);
window.addEventListener('mousemove', handleMove);
window.addEventListener('mouseup', () => { drawing = false; isPanning = false; initialPinchDistance = null; });

canvas.addEventListener('touchstart', (e) => { e.preventDefault(); startInteraction(e); }, { passive: false });
window.addEventListener('touchmove', (e) => { if (!e.target.closest('.lobby-overlay')) { e.preventDefault(); handleMove(e); } }, { passive: false });
window.addEventListener('touchend', () => { drawing = false; isPanning = false; initialPinchDistance = null; });

function drawLine(data) {
    if (!data) return;
    const lx = data.lastX ?? data.LastX, ly = data.lastY ?? data.LastY;
    const x = data.x ?? data.X, y = data.y ?? data.Y;
    const size = data.size ?? data.Size, isEraser = data.isEraser ?? data.IsEraser, color = data.color ?? data.Color;
    ctx.beginPath(); ctx.moveTo(lx, ly); ctx.lineTo(x, y);
    ctx.globalCompositeOperation = isEraser ? 'destination-out' : 'source-over';
    ctx.strokeStyle = color; ctx.lineWidth = size;
    ctx.stroke(); ctx.closePath();
    ctx.globalCompositeOperation = 'source-over';
}

function redrawCanvas() { ctx.clearRect(0, 0, canvas.width, canvas.height); strokeHistory.forEach(s => drawLine(s)); }
function clearLocal() { ctx.clearRect(0, 0, canvas.width, canvas.height); }

// Palette etc
const palette = document.getElementById('palette');
const paletteEditor = document.getElementById('palette-editor');
palette.onclick = (e) => { if (e.target.classList.contains('swatch')) { colorPicker.value = e.target.dataset.color; setTool('pencil'); } };

function undo() { if (isGameActive && !isDrawer) return; connection.invoke("UndoStroke"); }
document.getElementById('undo-btn').onclick = undo;
clearBtn.onclick = () => { if (isGameActive && !isDrawer) return; if (confirm('Clear?')) connection.invoke("ClearCanvas"); };

saveBtn.onclick = () => {
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = canvas.width; tempCanvas.height = canvas.height;
    const tCtx = tempCanvas.getContext('2d');
    const bg = document.getElementById('bg-layer');
    if (bg.src && bg.style.display !== 'none') {
        tCtx.globalAlpha = parseFloat(bg.style.opacity) || 0.5;
        tCtx.drawImage(bg, 0, 0, canvas.width, canvas.height);
        tCtx.globalAlpha = 1.0;
    }
    tCtx.drawImage(canvas, 0, 0);
    const link = document.createElement('a');
    link.download = `scribble-${Date.now()}.png`; link.href = tempCanvas.toDataURL(); link.click();
};

const toggleUiBtn = document.getElementById('toggle-ui');
toggleUiBtn.onclick = () => { document.body.classList.toggle('tools-hidden'); };

// BG
const bgUpload = document.getElementById('bg-upload');
const bgLayer = document.getElementById('bg-layer');
const bgOptions = document.getElementById('bg-options');
document.getElementById('add-bg-btn').onclick = () => { if (isGameActive) return; bgUpload.click(); };
bgUpload.onchange = (e) => {
    const reader = new FileReader();
    reader.onload = (ev) => {
        const b64 = ev.target.result;
        updateBackgroundLocal(b64);
        connection.invoke("UpdateBackground", b64);
    };
    reader.readAsDataURL(e.target.files[0]);
};
function updateBackgroundLocal(b64) {
    if (!b64) { bgLayer.src = ""; bgLayer.style.display = 'none'; bgOptions.style.display = 'none'; }
    else { bgLayer.src = b64; bgLayer.style.display = 'block'; bgOptions.style.display = 'flex'; }
}

window.onkeydown = (e) => {
    if (e.target.tagName === 'INPUT') return;
    if (e.code === 'Space') { spacePressed = true; viewport.style.cursor = 'grab'; }
    if (e.key.toLowerCase() === 'z' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); undo(); }
};
window.onkeyup = (e) => { if (e.code === 'Space') { spacePressed = false; setTool(currentTool); } };
window.onload = init;
window.onresize = applyTransform;
