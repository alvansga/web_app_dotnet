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

// --- INTERACTION HANDLERS ---
function startDrawing(e) {
    if (e.button === 1 || spacePressed || (e.touches && e.touches.length > 1)) {
        isPanning = true;
        if (e.touches) {
            if (e.touches.length === 2) {
                lastMidpoint = {
                    x: (e.touches[0].clientX + e.touches[1].clientX) / 2,
                    y: (e.touches[0].clientY + e.touches[1].clientY) / 2
                };
            }
            lastTouchPos = { x: e.touches[0].clientX, y: e.touches[0].clientY };
        }
        return;
    }
    
    drawing = true;
    const [x, y] = getCoordinates(e);
    lastX = x;
    lastY = y;
}

function stopDrawing() {
    drawing = false;
    isPanning = false;
    initialPinchDistance = null;
    lastMidpoint = null;
}

function move(e) {
    if (isPanning) {
        if (e.touches && e.touches.length === 2) {
            handlePinchAndPan(e);
            return;
        }
        
        // Single finger/mouse pan
        let dx, dy;
        if (e.touches) {
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

    if (!drawing) return;
    
    const [x, y] = getCoordinates(e);
    const drawData = {
        lastX, lastY, x, y,
        color: colorPicker.value,
        size: brushSizeRange.value
    };

    drawLine(drawData);
    if (connection.state === "Connected") {
        connection.invoke("DrawLine", drawData).catch(err => console.error(err));
    }
    [lastX, lastY] = [x, y];
}

// Pinch & Pan Combined (Smooth Midpoint logic)
function handlePinchAndPan(e) {
    const t1 = e.touches[0];
    const t2 = e.touches[1];
    
    const midX = (t1.clientX + t2.clientX) / 2;
    const midY = (t1.clientY + t2.clientY) / 2;
    const dist = Math.hypot(t1.clientX - t2.clientX, t1.clientY - t2.clientY);

    if (initialPinchDistance === null) {
        initialPinchDistance = dist;
        initialPinchScale = transform.scale;
        lastMidpoint = { x: midX, y: midY };
    } else {
        // 1. Handle Zoom
        const factor = dist / initialPinchDistance;
        const newScale = Math.min(Math.max(initialPinchScale * factor, 0.05), 10);
        
        // 2. Handle Pan (Apply movement of the midpoint)
        const dx = midX - lastMidpoint.x;
        const dy = midY - lastMidpoint.y;
        
        // Apply zoom at the NEW midpoint
        zoomAt(midX, midY, newScale);
        
        // Apply residual translation
        transform.x += dx;
        transform.y += dy;
        
        lastMidpoint = { x: midX, y: midY };
        applyTransform();
    }
}

function zoomAt(clientX, clientY, newScale) {
    const oldScale = transform.scale;
    const mouseX = clientX - transform.x;
    const mouseY = clientY - transform.y;
    
    const newX = clientX - (mouseX / oldScale) * newScale;
    const newY = clientY - (mouseY / oldScale) * newScale;
    
    transform.scale = newScale;
    transform.x = newX;
    transform.y = newY;
    // applyTransform() is called by the caller
}

// Mouse Wheel Zoom (Smoother Step)
viewport.addEventListener('wheel', (e) => {
    e.preventDefault();
    const delta = e.deltaY > 0 ? 0.95 : 1.05; // Smaller steps for comfort
    const newScale = Math.min(Math.max(transform.scale * delta, 0.05), 10);
    zoomAt(e.clientX, e.clientY, newScale);
    applyTransform();
}, { passive: false });

// Key Bindings
window.addEventListener('keydown', (e) => {
    if (e.code === 'Space') {
        spacePressed = true;
        viewport.style.cursor = 'grab';
    }
});

window.addEventListener('keyup', (e) => {
    if (e.code === 'Space') {
        spacePressed = false;
        viewport.style.cursor = 'crosshair';
    }
});

// Canvas Events
canvas.addEventListener('mousedown', startDrawing);
window.addEventListener('mousemove', move);
window.addEventListener('mouseup', stopDrawing);

canvas.addEventListener('touchstart', (e) => {
    if (e.touches.length > 0) startDrawing(e);
}, { passive: false });

window.addEventListener('touchmove', (e) => {
    if (e.touches.length > 0) move(e);
}, { passive: false });

window.addEventListener('touchend', stopDrawing);

// Draw logic
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

// Toolbar controls
colorPicker.addEventListener('input', () => {});
brushSizeRange.addEventListener('input', () => {
    sizeValueSpan.textContent = brushSizeRange.value;
});

clearBtn.addEventListener('click', () => {
    if (confirm('Bersihkan kanvas untuk semua orang?')) {
        connection.invoke("ClearCanvas").catch(e => console.error(e));
    }
});

saveBtn.addEventListener('click', () => {
    const link = document.createElement('a');
    link.download = `scribble-${Date.now()}.png`;
    link.href = canvas.toDataURL();
    link.click();
});

window.addEventListener('load', init);
window.addEventListener('resize', () => {
    // Keep transforms relevant after window resize
    applyTransform();
});
