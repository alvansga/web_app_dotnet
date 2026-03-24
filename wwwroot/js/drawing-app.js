"use strict";

const canvas = document.getElementById('drawing-canvas');
const ctx = canvas.getContext('2d');
const colorPicker = document.getElementById('color-picker');
const brushSizeRange = document.getElementById('brush-size');
const sizeValueSpan = document.getElementById('size-value');
const clearBtn = document.getElementById('clear-btn');
const saveBtn = document.getElementById('save-btn');
const connectionStatus = document.getElementById('connection-status');
const connectionText = document.getElementById('connection-text');

let drawing = false;
let lastX = 0;
let lastY = 0;
let currentSettings = {
    color: '#3a86ff',
    size: 5
};

// --- SIGNALR SETUP ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/drawingHub")
    .withAutomaticReconnect()
    .build();

connection.on("ReceiveDraw", (data) => {
    drawRemote(data);
});

connection.on("CanvasCleared", () => {
    clearLocal();
});

connection.onreconnecting(error => {
    updateStatus('offline', 'Reconnecting...');
});

connection.onreconnected(connectionId => {
    updateStatus('online', 'Connected');
});

connection.onclose(error => {
    updateStatus('offline', 'Disconnected');
});

async function startConnection() {
    try {
        await connection.start();
        updateStatus('online', 'Connected');
        console.log("SignalR Connected.");
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        updateStatus('offline', 'Connection Failed');
        setTimeout(startConnection, 5000);
    }
}

function updateStatus(status, text) {
    connectionStatus.className = `status-indicator ${status}`;
    connectionText.textContent = text;
}

startConnection();

// --- CANVAS LOGIC ---
function resizeCanvas() {
    // Save current drawing
    const tempImage = canvas.toDataURL();
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    
    // Fill background with white
    ctx.fillStyle = "white";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    // Restore drawing
    const img = new Image();
    img.src = tempImage;
    img.onload = () => {
        ctx.drawImage(img, 0, 0);
    };
    
    // Set drawing properties
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
}

window.addEventListener('resize', resizeCanvas);
resizeCanvas();

function startDrawing(e) {
    drawing = true;
    [lastX, lastY] = getCoordinates(e);
}

function stopDrawing() {
    drawing = false;
}

function draw(e) {
    if (!drawing) return;
    
    const [x, y] = getCoordinates(e);
    const drawData = {
        lastX, lastY, x, y,
        color: colorPicker.value,
        size: brushSizeRange.value
    };

    // Draw locally
    drawLine(drawData);

    // Send to other clients
    if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke("DrawLine", drawData).catch(err => console.error(err));
    }

    [lastX, lastY] = [x, y];
}

function getCoordinates(e) {
    let x, y;
    if (e.touches && e.touches.length > 0) {
        x = e.touches[0].clientX - canvas.offsetLeft;
        y = e.touches[0].clientY - canvas.offsetTop;
    } else {
        x = e.clientX - canvas.offsetLeft;
        y = e.clientY - canvas.offsetTop;
    }
    return [x, y];
}

function drawLine(data) {
    ctx.beginPath();
    ctx.moveTo(data.lastX, data.lastY);
    ctx.lineTo(data.x, data.y);
    ctx.strokeStyle = data.color;
    ctx.lineWidth = data.size;
    ctx.stroke();
    ctx.closePath();
}

function drawRemote(data) {
    drawLine(data);
}

function clearLocal() {
    ctx.fillStyle = "white";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
}

// Event Listeners
canvas.addEventListener('mousedown', startDrawing);
canvas.addEventListener('mousemove', draw);
canvas.addEventListener('mouseup', stopDrawing);
canvas.addEventListener('mouseout', stopDrawing);

// Touch Support
canvas.addEventListener('touchstart', (e) => {
    e.preventDefault();
    startDrawing(e);
}, { passive: false });

canvas.addEventListener('touchmove', (e) => {
    e.preventDefault();
    draw(e);
}, { passive: false });

canvas.addEventListener('touchend', (e) => {
    e.preventDefault();
    stopDrawing();
}, { passive: false });

// Toolbar controls
colorPicker.addEventListener('input', () => {
    currentSettings.color = colorPicker.value;
});

brushSizeRange.addEventListener('input', () => {
    const size = brushSizeRange.value;
    sizeValueSpan.textContent = size;
    currentSettings.size = size;
});

clearBtn.addEventListener('click', () => {
    if (confirm('Clear the entire canvas for everyone?')) {
        connection.invoke("ClearCanvas").catch(err => console.error(err));
    }
});

saveBtn.addEventListener('click', () => {
    const link = document.createElement('a');
    link.download = `scribble-${Date.now()}.png`;
    link.href = canvas.toDataURL();
    link.click();
});
