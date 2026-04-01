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

const WORDS = [
    // MARVEL & DC
    "IRON MAN", "SPIDER-MAN", "THOR", "HULK", "BLACK WIDOW", "CAPTAIN AMERICA", "GROOT", "THANOS", "LOKI", "WOLVERINE",
    "BATMAN", "SUPERMAN", "WONDER WOMAN", "THE FLASH", "AQUAMAN", "JOKER", "HARLEY QUINN", "BLACK PANTHER", "DOCTOR STRANGE", "VENOM",

    // DISNEY & PIXAR
    "MICKEY MOUSE", "DONALD DUCK", "GOOFY", "ELSA", "ANNA", "OLAF", "SIMBA", "ALADDIN", "GENIE", "ARIEL",
    "MULAN", "STITCH", "BAYMAX", "WINNIE THE POOH", "MALEFICENT", "PETER PAN", "HERCULES", "MOANA", "MAUI", "RAPUNZEL",
    "WOODY", "BUZZ LIGHTYEAR", "NEMO", "DORY", "WALL-E", "REMY", "SULLY", "MIKE WAZOWSKI", "LIGHTNING MCQUEEN", "MATER",
    "MR. INCREDIBLE", "ELASTIGIRL", "JOY", "SADNESS", "BING BONG", "RUSSELL", "CARL FREDRICKSEN",

    // ANIME & MANGA
    "NARUTO", "SASUKE", "KAKASHI", "ITACHI", "GAARA", "KURAMA", "HINATA", "MADARA", "TSUNADE", "JIRAIYA",
    "LUFFY", "ZORO", "NAMI", "SANJI", "CHOPPER", "ROBIN", "BROOK", "SHANKS", "ACE", "KAIDO",
    "GOKU", "VEGETA", "FRIEZA", "CELL", "MAJIN BUU", "PIKACHU", "CHARIZARD", "DORAEMON", "TOTORO", "SAITAMA",
    "TANJIRO", "NEZUKO", "ZENITSU", "INOSUKE", "MUZAN", "RENGOKU", "LIGHT YAGAMI", "RYUK", "EDWARD ELRIC", "ALPHONSE ELRIC",

    // MY HERO ACADEMIA
    "DEKU", "ALL MIGHT", "BAKUGO", "TODOROKI", "URARAKA", "IIDA", "FROPPY", "KIRISHIMA", "ENDEAVOR", "ERASERHEAD",
    "SHIGARAKI", "TOGA", "DABI", "ALL FOR ONE", "MIRIO",

    // ATTACK ON TITAN
    "EREN YEAGER", "MIKASA ACKERMAN", "LEVI ACKERMAN", "ARMIN ARLERT", "ERWIN SMITH", "REINER BRAUN", "BERTHOLDT", "ZEKE YEAGER",
    "COLOSSAL TITAN", "ARMORED TITAN", "BEAST TITAN", "FEMALE TITAN", "JAW TITAN",

    // NICKELODEON
    "SPONGEBOB", "PATRICK STAR", "SQUIDWARD", "MR. KRABS", "SANDY CHEEKS", "PLANKTON", "GARY THE SNAIL",
    "AANG", "KATARA", "SOKKA", "ZUKO", "TOPH", "APPA", "MOMO", "KORRA",
    "DANNY PHANTOM", "TIMMY TURNER", "COSMO", "WANDA", "JIMMY NEUTRON", "ARNOLD SHORTMAN", "CATDOG",

    // CARTOON NETWORK
    "FINN THE HUMAN", "JAKE THE DOG",
    "BEN 10", "GWEN TENNYSON", "KEVIN LEVIN", "BLOSSOM", "BUBBLES", "BUTTERCUP",
];

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

        // Validation: Limit name length
        const name = (playerName || "Artist").substring(0, 15);

        isGameRunning = true;
        currentDrawerId = socket.id;
        currentDrawerName = name;
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

        // Security: Limit guess length
        const safeGuess = (guess || "").substring(0, 50).trim();
        const safeName = (playerName || "Artist").substring(0, 15);

        const correct = safeGuess.toUpperCase() === currentWord.toUpperCase();
        io.emit("ReceiveMessage", safeName, safeGuess, correct);

        if (correct) {
            endGame(safeName, currentWord);
        }
    });

    socket.on('DrawLine', (data) => {
        // Validation: Only drawer can draw
        if (!isGameRunning || socket.id !== currentDrawerId) return;

        // Sanitize data: Ensure it's not a massive object
        if (strokeHistory.length > 50000) return; // Prevent memory leak DoS

        strokeHistory.push(data);
        socket.broadcast.emit("ReceiveDraw", data);
    });

    socket.on('UndoStroke', () => {
        // Validation: Only drawer can undo
        if (!isGameRunning || socket.id !== currentDrawerId) return;

        if (strokeHistory.length === 0) return;
        const lastStrokeId = strokeHistory[strokeHistory.length - 1].strokeId;
        strokeHistory = strokeHistory.filter(s => s.strokeId !== lastStrokeId);
        io.emit("StrokeUndone", lastStrokeId);
    });

    socket.on('ClearCanvas', () => {
        // Validation: Only drawer can clear
        if (isGameRunning && socket.id !== currentDrawerId) return;

        strokeHistory = [];
        backgroundBase64 = "";
        io.emit("CanvasCleared");
    });

    socket.on('UpdateBackground', (base64) => {
        // Validation: No background change during game
        if (isGameRunning) return;

        // Security: Limit background size (5MB max for base64)
        if (base64 && base64.length > 5 * 1024 * 1024) return;

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
