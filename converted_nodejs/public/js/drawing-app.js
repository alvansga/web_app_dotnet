"use strict";

const socket = io();

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

// --- APP STATE ---
let drawing = false;
let lastX = 0;
let lastY = 0;
let isPanning = false;
let spacePressed = false;

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

const playerNameInput = document.getElementById('player-name');
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

const WORLD_SIZE = 2500;
canvas.width = WORLD_SIZE;
canvas.height = WORLD_SIZE;

function init() {
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    
    if (!playerNameInput.value) {
        playerNameInput.value = "Artist" + Math.floor(Math.random() * 1000);
    }

    const vw = viewport.clientWidth;
    const vh = viewport.clientHeight;
    transform.x = (vw - WORLD_SIZE * transform.scale) / 2;
    transform.y = (vh - WORLD_SIZE * transform.scale) / 2;
    applyTransform();
}

function applyTransform() {
    wrapper.style.transform = `translate(${transform.x}px, ${transform.y}px) scale(${transform.scale})`;
}

// --- SOCKET.IO EVENT HANDLERS ---
socket.on("ReceiveDraw", (data) => {
    strokeHistory.push(data);
    drawLine(data);
});

socket.on("CanvasCleared", () => {
    strokeHistory = [];
    clearLocal();
    if (bgLayer) {
        bgLayer.src = "";
        bgLayer.style.display = 'none';
        bgOptions.style.display = 'none';
    }
});

socket.on("ReceiveBackground", (base64) => {
    updateBackgroundLocal(base64);
});

socket.on("LoadHistory", (history) => {
    strokeHistory = Array.isArray(history) ? history : [];
    redrawCanvas();
});

socket.on("StrokeUndone", (strokeId) => {
    strokeHistory = strokeHistory.filter(s => {
        const sid = s.strokeId || s.StrokeId;
        return sid !== strokeId;
    });
    redrawCanvas();
});

socket.on("GameStarted", (data) => {
    isGameActive = true;
    isDrawer = (socket.id === data.drawerId);
    
    strokeHistory = [];
    clearLocal();
    
    gameOverlay.style.display = 'block';
    guessPanel.style.display = 'block';
    playBtn.style.display = 'none';
    wordDisplay.style.display = isDrawer ? 'block' : 'none';
    drawerInfo.textContent = isDrawer ? "YOU are drawing!" : `${data.drawerName} is drawing...`;
    gameMessages.innerHTML = `<div class="msg system">Game started! ${data.drawerName} is drawing.</div>`;
    
    startLocalTimer(new Date(data.endTime));
    
    if (isDrawer) {
        setTool('pencil');
    } else {
        setTool('move');
        guessInput.focus();
    }
});

socket.on("ReceiveWord", (word) => {
    targetWordSpan.textContent = word;
});

socket.on("ReceiveMessage", (sender, message, isCorrect) => {
    const msgDiv = document.createElement('div');
    msgDiv.className = isCorrect ? 'msg correct' : 'msg';
    msgDiv.innerHTML = `<span class="sender">${sender}:</span> ${message}`;
    gameMessages.prepend(msgDiv);
});

socket.on("GameEnded", (data) => {
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

socket.on('connect', () => {
    updateStatus('online', 'Connected');
});

socket.on('disconnect', () => {
    updateStatus('offline', 'Disconnected');
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

// Actions
playBtn.onclick = () => {
    const name = playerNameInput.value.trim() || "Artist";
    socket.emit("StartGame", name);
};

function sendGuess() {
    const guess = guessInput.value.trim();
    if (!guess || !isGameActive || isDrawer) return;
    
    const name = playerNameInput.value.trim() || "Artist";
    socket.emit("MakeGuess", guess, name);
    guessInput.value = "";
}

guessInput.onkeydown = (e) => { if (e.key === "Enter") sendGuess(); };
sendGuessBtn.onclick = sendGuess;

function updateStatus(status, text) {
    if (!connectionStatus) return;
    connectionStatus.className = `status-indicator ${status}`;
    connectionText.textContent = text;
}

// --- COORDINATE MAPPING ---
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

// --- TOOL STATE ---
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

// --- INTERACTION HANDLERS ---
function startInteraction(e) {
    const isTouch = e.touches && e.touches.length > 0;
    const touchCount = isTouch ? e.touches.length : 1;

    if (touchCount === 2) {
        isPanning = true;
        drawing = false;
        const t1 = e.touches[0];
        const t2 = e.touches[1];
        initialPinchDistance = Math.hypot(t1.clientX - t2.clientX, t1.clientY - t2.clientY);
        initialPinchScale = transform.scale;
        lastMidpoint = { 
            x: (t1.clientX + t2.clientX) / 2, 
            y: (t1.clientY + t2.clientY) / 2 
        };
        return;
    }

    if (currentTool === 'move' || e.button === 1 || spacePressed || (isGameActive && !isDrawer)) {
        isPanning = true;
        drawing = false;
        if (isTouch) {
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        }
    } else {
        drawing = true;
        isPanning = false;
        currentStrokeId = Date.now().toString() + Math.random().toString(36).substr(2, 9);
        const [x, y] = getCoordinates(e);
        lastX = x;
        lastY = y;
    }
}

function stopInteraction() {
    drawing = false;
    isPanning = false;
    initialPinchDistance = null;
    lastMidpoint = null;
    currentStrokeId = null;
}

function handleMove(e) {
    const isTouch = e.touches && e.touches.length > 0;
    const touchCount = isTouch ? e.touches.length : 1;

    if (isTouch && touchCount === 2) {
        handlePinchOnly(e);
        return;
    }

    if (isPanning) {
        let dx, dy;
        if (isTouch) {
            dx = e.touches[0].clientX - lastTouchPos.x;
            dy = e.touches[0].clientY - lastTouchPos.y;
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        } else {
            dx = e.movementX;
            dy = e.movementY;
        }
        transform.x += dx;
        transform.y += dy;
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
        if (socket.connected) {
            socket.emit("DrawLine", drawData);
        }
        [lastX, lastY] = [x, y];
    }
}

function handlePinchOnly(e) {
    const t1 = e.touches[0];
    const t2 = e.touches[1];
    const midX = (t1.clientX + t2.clientX) / 2;
    const midY = (t1.clientY + t2.clientY) / 2;
    const dist = Math.hypot(t1.clientX - t2.clientX, t1.clientY - t2.clientY);

    if (initialPinchDistance === null) {
        initialPinchDistance = dist;
        initialPinchScale = transform.scale;
    } else {
        const factor = dist / initialPinchDistance;
        const newScale = Math.min(Math.max(initialPinchScale * factor, 0.05), 10);
        zoomAt(midX, midY, newScale);
        applyTransform();
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
window.addEventListener('mouseup', stopInteraction);

canvas.addEventListener('touchstart', (e) => {
    e.preventDefault();
    startInteraction(e);
}, { passive: false });

window.addEventListener('touchmove', (e) => {
    if (e.target.closest('.guess-panel') || e.target.closest('.side-panel')) return; 
    e.preventDefault();
    handleMove(e);
}, { passive: false });

window.addEventListener('touchend', stopInteraction);

document.getElementById('game-messages').style.overscrollBehavior = 'contain';
document.getElementById('palette').style.overscrollBehavior = 'contain';

function drawLine(data) {
    if (!data) return;
    const lx = data.lastX !== undefined ? data.lastX : data.LastX;
    const ly = data.lastY !== undefined ? data.lastY : data.LastY;
    const x = data.x !== undefined ? data.x : data.X;
    const y = data.y !== undefined ? data.y : data.Y;
    const size = data.size !== undefined ? data.size : data.Size;
    const isEraser = data.isEraser !== undefined ? data.isEraser : data.IsEraser;
    const color = data.color !== undefined ? data.color : data.Color;

    ctx.beginPath();
    ctx.moveTo(lx, ly);
    ctx.lineTo(x, y);
    ctx.globalCompositeOperation = isEraser ? 'destination-out' : 'source-over';
    ctx.strokeStyle = color;
    ctx.lineWidth = size;
    ctx.stroke();
    ctx.closePath();
    ctx.globalCompositeOperation = 'source-over';
}

function redrawCanvas() {
    clearLocal();
    strokeHistory.forEach(s => drawLine(s));
}

function clearLocal() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}

const palette = document.getElementById('palette');
const paletteEditor = document.getElementById('palette-editor');
let longPressTimer;
let currentEditingSwatch = null;

palette.onclick = (e) => {
    if (e.target.classList.contains('swatch')) {
        const color = e.target.dataset.color;
        colorPicker.value = color;
        setTool('pencil');
    }
};

palette.oncontextmenu = (e) => {
    if (e.target.classList.contains('swatch')) {
        e.preventDefault();
        openSwatchEditor(e.target);
    }
};

palette.addEventListener('touchstart', (e) => {
    if (e.target.classList.contains('swatch')) {
        longPressTimer = setTimeout(() => {
            openSwatchEditor(e.target);
            longPressTimer = null;
        }, 600);
    }
}, { passive: true });

palette.addEventListener('touchend', () => {
    if (longPressTimer) { clearTimeout(longPressTimer); longPressTimer = null; }
});

palette.addEventListener('touchmove', () => {
    if (longPressTimer) { clearTimeout(longPressTimer); longPressTimer = null; }
});

function openSwatchEditor(swatch) {
    currentEditingSwatch = swatch;
    paletteEditor.value = swatch.dataset.color;
    paletteEditor.click();
}

paletteEditor.oninput = () => {
    if (currentEditingSwatch) {
        const newColor = paletteEditor.value;
        currentEditingSwatch.style.background = newColor;
        currentEditingSwatch.dataset.color = newColor;
        colorPicker.value = newColor;
        setTool('pencil');
    }
};

colorPicker.oninput = () => { if (currentTool === 'eraser') setTool('pencil'); };
brushSizeRange.oninput = () => { sizeValueSpan.textContent = brushSizeRange.value; };

const undoBtn = document.getElementById('undo-btn');
undoBtn.onclick = () => {
    if (isGameActive && !isDrawer) return;
    socket.emit("UndoStroke");
};

clearBtn.onclick = () => {
    if (isGameActive && !isDrawer) return;
    if (confirm('Clear canvas for everyone?')) socket.emit("ClearCanvas");
};

saveBtn.onclick = () => {
    const tempCanvas = document.createElement('canvas');
    tempCanvas.width = canvas.width;
    tempCanvas.height = canvas.height;
    const tCtx = tempCanvas.getContext('2d');
    if (bgLayer.src && bgLayer.style.display !== 'none') {
        tCtx.globalAlpha = parseFloat(bgLayer.style.opacity) || 0.5;
        const ratio = Math.min(tempCanvas.width / bgLayer.naturalWidth, tempCanvas.height / bgLayer.naturalHeight);
        const drawW = bgLayer.naturalWidth * ratio;
        const drawH = bgLayer.naturalHeight * ratio;
        tCtx.drawImage(bgLayer, (tempCanvas.width - drawW) / 2, (tempCanvas.height - drawH) / 2, drawW, drawH);
        tCtx.globalAlpha = 1.0;
    }
    tCtx.drawImage(canvas, 0, 0);
    const link = document.createElement('a');
    link.download = `scribble-${Date.now()}.png`;
    link.href = tempCanvas.toDataURL();
    link.click();
};

const toggleUiBtn = document.getElementById('toggle-ui');
let uiHidden = false;
function toggleUI() {
    uiHidden = !uiHidden;
    document.body.classList.toggle('tools-hidden', uiHidden);
    toggleUiBtn.querySelector('i').className = uiHidden ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye';
    toggleUiBtn.classList.toggle('active', uiHidden);
}
toggleUiBtn.onclick = toggleUI;

window.onload = init;
window.onresize = applyTransform;

const addBgBtn = document.getElementById('add-bg-btn');
const bgUpload = document.getElementById('bg-upload');
const bgLayer = document.getElementById('bg-layer');
const bgOptions = document.getElementById('bg-options');
const bgOpacityRange = document.getElementById('bg-opacity');

if (addBgBtn) {
    addBgBtn.onclick = () => {
        if (isGameActive) return alert("Background changes are disabled during a game!");
        if (bgLayer.src && bgLayer.style.display !== 'none') {
            if (confirm("Remove background for everyone?")) {
                bgLayer.src = ""; bgLayer.style.display = 'none'; bgOptions.style.display = 'none';
                addBgBtn.querySelector('i').className = 'fa-solid fa-layer-group';
                socket.emit("UpdateBackground", "");
            }
        } else bgUpload.click();
    };
}

if (bgUpload) {
    bgUpload.addEventListener('change', (e) => {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = (event) => {
                const base64 = event.target.result;
                updateBackgroundLocal(base64);
                socket.emit("UpdateBackground", base64);
            };
            reader.readAsDataURL(file);
        }
    });
}

function updateBackgroundLocal(base64) {
    if (!base64) {
        bgLayer.src = ""; bgLayer.style.display = 'none'; bgOptions.style.display = 'none';
        addBgBtn.querySelector('i').className = 'fa-solid fa-layer-group';
    } else {
        bgLayer.src = base64; bgLayer.style.display = 'block'; bgOptions.style.display = 'flex';
        addBgBtn.querySelector('i').className = 'fa-solid fa-trash-can';
    }
}

if (bgOpacityRange) {
    bgOpacityRange.oninput = () => { if (bgLayer) bgLayer.style.opacity = bgOpacityRange.value; };
}

window.onkeydown = (e) => {
    if (e.code === 'Space') { spacePressed = true; viewport.style.cursor = 'grab'; }
    if (e.code === 'Tab') { e.preventDefault(); toggleUI(); }
    if (e.key.toLowerCase() === 'z' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); socket.emit("UndoStroke"); }
    if (e.key.toLowerCase() === 'b') setTool('pencil');
    if (e.key.toLowerCase() === 'e') setTool('eraser');
    if (e.key.toLowerCase() === 'h') setTool('move');
};
window.onkeyup = (e) => { if (e.code === 'Space') { spacePressed = false; setTool(currentTool); } };
