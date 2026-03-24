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

// --- APP STATE ---
let drawing = false;
let lastX = 0;
let lastY = 0;
let isPanning = false;
let spacePressed = false;

// Viewport Transform
let transform = {
    x: 0,
    y: 0,
    scale: 0.8 // Start slightly zoomed out
};

// Multi-touch tracking
let initialPinchDistance = null;
let initialPinchScale = 1;
let lastMidpoint = { x: 0, y: 0 };
let lastTouchPos = { x: 0, y: 0 };

// World Config
const WORLD_SIZE = 2500;
canvas.width = WORLD_SIZE;
canvas.height = WORLD_SIZE;

// --- INITIALIZE ---
function init() {
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.fillStyle = "white";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
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

connection.on("ReceiveDraw", (data) => drawLine(data));
connection.on("CanvasCleared", () => clearLocal());
connection.on("LoadHistory", (history) => {
    // Clear and redraw everything from history
    clearLocal(); 
    history.forEach(stroke => drawLine(stroke));
});

connection.start().then(() => updateStatus('online', 'Connected')).catch(e => updateStatus('offline', 'Error'));

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
let currentTool = 'pencil'; // 'pencil', 'eraser', 'move'
let lastSelectedColor = '#3a86ff';

const pencilBtn = document.getElementById('pencil-tool');
const eraserBtn = document.getElementById('eraser-tool');
const moveBtn = document.getElementById('move-tool');

function setTool(tool) {
    currentTool = tool;
    pencilBtn.classList.toggle('active', tool === 'pencil');
    eraserBtn.classList.toggle('active', tool === 'eraser');
    moveBtn.classList.toggle('active', tool === 'move');
    
    // Set appropriate cursor
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

    // Reset multi-touch state
    if (touchCount === 2) {
        isPanning = true;
        drawing = false; // Never draw with 2 fingers
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

    // 1-Finger/Mouse logic
    if (currentTool === 'move' || e.button === 1 || spacePressed) {
        isPanning = true;
        drawing = false;
        if (isTouch) {
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        }
    } else {
        drawing = true;
        isPanning = false;
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
}

function handleMove(e) {
    const isTouch = e.touches && e.touches.length > 0;
    const touchCount = isTouch ? e.touches.length : 1;

    // Handle 2-Finger Zoom ONLY
    if (isTouch && touchCount === 2) {
        handlePinchOnly(e);
        return;
    }

    // Handle Panning (1 finger or mouse)
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

    // Handle Drawing
    if (drawing) {
        const [x, y] = getCoordinates(e);
        const color = (currentTool === 'eraser') ? '#ffffff' : colorPicker.value;
        const drawData = {
            lastX, lastY, x, y,
            color: color,
            size: brushSizeRange.value
        };

        drawLine(drawData);
        if (connection.state === "Connected") {
            connection.invoke("DrawLine", drawData).catch(err => console.error(err));
        }
        [lastX, lastY] = [x, y];
    }
}

// Pinch & Zoom Only (Translation component removed for 2 fingers)
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
        
        // Zoom centered at midpoint, but NO additional translation (dx/dy)
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

// Event Listeners
viewport.addEventListener('wheel', (e) => {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.95 : 1.05;
    zoomAt(e.clientX, e.clientY, transform.scale * delta);
    applyTransform();
}, { passive: false });

// Global Events
canvas.addEventListener('mousedown', startInteraction);
window.addEventListener('mousemove', handleMove);
window.addEventListener('mouseup', stopInteraction);

canvas.addEventListener('touchstart', (e) => {
    e.preventDefault();
    startInteraction(e);
}, { passive: false });

window.addEventListener('touchmove', (e) => {
    e.preventDefault();
    handleMove(e);
}, { passive: false });

window.addEventListener('touchend', stopInteraction);

// Utilities
function drawLine(data) {
    ctx.beginPath();
    ctx.moveTo(data.lastX, data.lastY);
    ctx.lineTo(data.x, data.y);
    ctx.strokeStyle = data.color;
    ctx.lineWidth = data.size;
    ctx.stroke();
    ctx.closePath();
}

function clearLocal() {
    ctx.fillStyle = "white";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
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

// --- PALETTE EDITING (Right Click & Long Press) ---
palette.oncontextmenu = (e) => {
    if (e.target.classList.contains('swatch')) {
        e.preventDefault();
        openSwatchEditor(e.target);
    }
};

// Long Press for Android/iOS
palette.addEventListener('touchstart', (e) => {
    if (e.target.classList.contains('swatch')) {
        longPressTimer = setTimeout(() => {
            openSwatchEditor(e.target);
            longPressTimer = null;
        }, 600); // 600ms for long press
    }
}, { passive: true });

palette.addEventListener('touchend', () => {
    if (longPressTimer) {
        clearTimeout(longPressTimer);
        longPressTimer = null;
    }
});

palette.addEventListener('touchmove', () => {
    if (longPressTimer) {
        clearTimeout(longPressTimer);
        longPressTimer = null;
    }
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
        // Optionally select it immediately
        colorPicker.value = newColor;
        setTool('pencil');
    }
};

// Tooling & Actions
colorPicker.oninput = () => { if (currentTool === 'eraser') setTool('pencil'); };
brushSizeRange.oninput = () => { sizeValueSpan.textContent = brushSizeRange.value; };

clearBtn.onclick = () => {
    if (confirm('Clear canvas for everyone?')) connection.invoke("ClearCanvas");
};

saveBtn.onclick = () => {
    const link = document.createElement('a');
    link.download = `scribble-${Date.now()}.png`;
    link.href = canvas.toDataURL();
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

window.onkeydown = (e) => {
    if (e.code === 'Space') { spacePressed = true; viewport.style.cursor = 'grab'; }
    if (e.code === 'Tab') { e.preventDefault(); toggleUI(); }
    if (e.key.toLowerCase() === 'b') setTool('pencil');
    if (e.key.toLowerCase() === 'e') setTool('eraser');
    if (e.key.toLowerCase() === 'h') setTool('move');
};

window.onkeyup = (e) => {
    if (e.code === 'Space') { spacePressed = false; setTool(currentTool); }
};

window.onload = init;
window.onresize = applyTransform;
