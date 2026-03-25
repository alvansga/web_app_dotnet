const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const path = require('path');

const app = express();
const server = http.createServer(app);
const io = new Server(server, {
    maxHttpBufferSize: 1e7 // 10MB to handle large background images
});

// App State
let isGameRunning = false;
let currentWord = "";
let currentDrawerId = null;
let currentDrawerName = "";
let gameTimer = null;
let strokeHistory = [];
let backgroundBase64 = "";

const WORDS = ["CAT", "DOG", "SUN", "HOUSE", "TREE", "CAR", "BIRD", "FLOWER", "MOUNTAIN", "OCEAN", "AIRPLANE", "DINOSAUR", "PIZZA", "GUITAR", "ASTRONAUT", "VOLCANO", "OCTOPUS", "RAINBOW", "FIRETRUCK"];

app.use(express.static(path.join(__dirname, 'public')));

io.on('connection', (socket) => {
    console.log('User connected:', socket.id);

    // Sync new client
    socket.emit("LoadHistory", strokeHistory);
    socket.emit("ReceiveBackground", backgroundBase64);
    
    if (isGameRunning) {
        socket.emit("GameStarted", {
            drawerId: currentDrawerId,
            drawerName: currentDrawerName,
            endTime: gameTimer ? gameTimer.endTime : null
        });
    }

    socket.on('StartGame', (playerName) => {
        if (isGameRunning) return;

        isGameRunning = true;
        currentDrawerId = socket.id;
        currentDrawerName = playerName;
        currentWord = WORDS[Math.floor(Math.random() * WORDS.length)];
        strokeHistory = [];
        backgroundBase64 = "";

        const endTime = new Date(Date.now() + 60000);
        gameTimer = {
            endTime: endTime,
            timeout: setTimeout(() => endGame(null, currentWord), 60000)
        };

        io.emit("CanvasCleared");
        io.emit("GameStarted", {
            drawerId: currentDrawerId,
            drawerName: currentDrawerName,
            endTime: endTime
        });

        socket.emit("ReceiveWord", currentWord);
    });

    socket.on('MakeGuess', (guess, playerName) => {
        if (!isGameRunning || socket.id === currentDrawerId) return;

        const correct = guess.toUpperCase().trim() === currentWord.toUpperCase();
        io.emit("ReceiveMessage", playerName, guess, correct);

        if (correct) {
            endGame(playerName, currentWord);
        }
    });

    socket.on('DrawLine', (data) => {
        if (isGameRunning && socket.id !== currentDrawerId) return;
        strokeHistory.push(data);
        socket.broadcast.emit("ReceiveDraw", data);
    });

    socket.on('UndoStroke', () => {
        if (isGameRunning && socket.id !== currentDrawerId) return;
        
        // Find last stroke from this user or just global last stroke
        // To mirror the C# logic, we'll find the last stroke ID
        if (strokeHistory.length === 0) return;
        const lastStrokeId = strokeHistory[strokeHistory.length - 1].strokeId;
        strokeHistory = strokeHistory.filter(s => s.strokeId !== lastStrokeId);
        io.emit("StrokeUndone", lastStrokeId);
    });

    socket.on('ClearCanvas', () => {
        if (isGameRunning && socket.id !== currentDrawerId) return;
        strokeHistory = [];
        backgroundBase64 = "";
        io.emit("CanvasCleared");
    });

    socket.on('UpdateBackground', (base64) => {
        if (isGameRunning) return;
        backgroundBase64 = base64;
        socket.broadcast.emit("ReceiveBackground", base64);
    });

    socket.on('disconnect', () => {
        console.log('User disconnected:', socket.id);
        if (socket.id === currentDrawerId && isGameRunning) {
            endGame(null, currentWord);
        }
    });

    function endGame(winnerName, word) {
        if (!isGameRunning) return;
        
        isGameRunning = false;
        if (gameTimer) {
            clearTimeout(gameTimer.timeout);
            gameTimer = null;
        }

        io.emit("GameEnded", {
            winnerName: winnerName,
            word: word
        });
        
        currentDrawerId = null;
        currentDrawerName = "";
        currentWord = "";
    }
});

const PORT = process.env.PORT || 5253;
server.listen(PORT, () => {
    console.log(`Node.js server running at http://localhost:${PORT}`);
    console.log(`To start the game, open http://localhost:${PORT} and click Play`);
});
