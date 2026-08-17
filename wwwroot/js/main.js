const { createApp, reactive, computed, toRefs } = Vue;

const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/game')
  .withAutomaticReconnect()
  .build();

const NAME_KEY = 'organ_attack_player_name';

createApp({
  setup() {
    const savedName = localStorage.getItem(NAME_KEY) || '';

    const state = reactive({
      connectionStatus: 'Menghubungkan...',
      view: savedName ? 'lobby' : 'setup',
      roomId: null,
      error: '',
      playerName: savedName,
      joinRoomId: '',
      rooms: [],
      players: [],
      hand: [],
      turn: null,
      deckCount: 0,
      discardCount: 0,
      winnerId: null,
      selectedCardId: null,
      swapMode: false,
      swapSelectedIds: [],
      log: [],
      myId: null,
    });

    const me = computed(() => state.players.find(p => p.id === state.myId) || null);
    const opponent = computed(() => state.players.find(p => p.id !== state.myId) || null);

    const myOrgans = computed(() => me.value ? me.value.organs : []);
    const opponentOrgans = computed(() => opponent.value ? opponent.value.organs : []);

    const isMyTurn = computed(() =>
      state.turn && me.value && !state.winnerId &&
      state.turn.currentPlayerId === me.value.id
    );

    const canAct = computed(() =>
      isMyTurn.value && !state.winnerId &&
      state.turn && state.turn.actionsUsed < state.turn.maxActionsPerTurn
    );

    const canDraw = computed(() =>
      isMyTurn.value && !state.winnerId &&
      state.turn && !state.turn.hasDrawnThisTurn &&
      state.hand.length < 5
    );

    const canSwap = computed(() =>
      canAct.value && state.turn && !state.turn.hasDrawnThisTurn
    );

    const swapCanConfirm = computed(() =>
      canSwap.value && state.swapSelectedIds.length >= 1 && state.swapSelectedIds.length <= 2
    );

    const currentTurnName = computed(() => {
      if (!state.turn) return '-';
      const p = state.players.find(p => p.id === state.turn.currentPlayerId);
      return p ? p.name : state.turn.currentPlayerId;
    });

    const winnerName = computed(() => {
      const p = state.players.find(p => p.id === state.winnerId);
      return p ? p.name : '';
    });

    let logId = 1;
    function log(message) {
      state.log.unshift({
        id: logId++,
        time: new Date().toLocaleTimeString(),
        message,
      });
      if (state.log.length > 100) state.log.length = 100;
    }

    function setError(message) {
      state.error = message;
      if (message) {
        setTimeout(() => { state.error = ''; }, 4000);
      }
    }

    const cardEmojiMap = {
      Affliction: '🦠',
      Attack: '⚔️',
      Treatment: '💊',
      Defense: '🛡️',
      Special: '✨',
    };

    const organImageMap = {
      heart: 'heart.png',
      brain: 'brain.png',
      lungs: 'lungs.png',
      liver: 'liver.png',
      teeth: 'teeth.png',
      kidneys: 'kidneys.png',
    };

    function organImage(o) {
      const name = (o.type || '').toLowerCase();
      return `assets/organs/${organImageMap[name] || 'placeholder.png'}`;
    }

    function organLabel(type) {
      if (!type) return '';
      // Insert a space before capital letters and replace underscores.
      return type
        .replace(/_/g, ' ')
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/\b\w/g, c => c.toUpperCase());
    }

    function cardEmoji(c) {
      return cardEmojiMap[c.type] || '🃏';
    }

    function isTargetable(player, organ) {
      if (!state.selectedCardId || !player || !me.value) return false;
      const card = state.hand.find(c => c.id === state.selectedCardId);
      if (!card) return false;

      if (card.targetSide === 'Self') {
        return player.id === me.value.id;
      }
      if (card.targetSide === 'Opponent') {
        return player.id !== me.value.id;
      }
      return false;
    }

    async function invoke(method, ...args) {
      try {
        await connection.invoke(method, ...args);
      } catch (err) {
        setError(err.message || String(err));
      }
    }

    // ---- Name setup ----

    function saveName() {
      const name = state.playerName.trim();
      if (!name) return;
      localStorage.setItem(NAME_KEY, name);
      state.playerName = name;
      state.view = 'lobby';
      refreshRooms();
    }

    // ---- Rooms ----

    function createRoom() {
      invoke('CreateRoom', state.playerName.trim());
    }

    function joinRoom() {
      const code = state.joinRoomId.trim();
      if (!code) return;
      invoke('JoinRoom', code, state.playerName.trim());
    }

    function joinKnownRoom(roomId) {
      invoke('JoinRoom', roomId, state.playerName.trim());
    }

    async function refreshRooms() {
      try {
        state.rooms = await connection.invoke('ListRooms');
      } catch (err) {
        setError(err.message || String(err));
      }
    }

    function startGame() {
      invoke('StartGame');
    }

    function drawCard() {
      invoke('DrawCard');
    }

    function endTurn() {
      invoke('EndTurn');
    }

    function clickCard(card) {
      if (!isMyTurn.value) return;

      if (state.swapMode) {
        // Toggle card in/out of the swap selection (max 2).
        const idx = state.swapSelectedIds.indexOf(card.id);
        if (idx >= 0) {
          state.swapSelectedIds.splice(idx, 1);
        } else if (state.swapSelectedIds.length < 2) {
          state.swapSelectedIds.push(card.id);
        }
        return;
      }

      state.selectedCardId = state.selectedCardId === card.id ? null : card.id;
    }

    function toggleSwapMode() {
      if (!canSwap.value) return;
      state.swapMode = !state.swapMode;
      state.swapSelectedIds.length = 0;
      state.selectedCardId = null;
    }

    function confirmSwap() {
      if (!swapCanConfirm.value) return;
      invoke('SwapCards', state.swapSelectedIds.slice());
      state.swapMode = false;
      state.swapSelectedIds.length = 0;
      state.selectedCardId = null;
    }

    function clickOrgan(player, organ) {
      if (!state.selectedCardId || !player || organ.isDestroyed) return;

      const card = state.hand.find(c => c.id === state.selectedCardId);
      if (!card) return;

      if (card.targetSide === 'Self' && player.id !== me.value.id) return;
      if (card.targetSide === 'Opponent' && player.id === me.value.id) return;

      invoke('PlayCard', card.id, player.id, organ.type);
      state.selectedCardId = null;
    }

    connection.on('PlayerJoined', (player) => {
      const idx = state.players.findIndex(p => p.id === player.id);
      if (idx >= 0) state.players[idx] = player;
      else state.players.push(player);
      log(`${player.name} bergabung.`);
    });

    connection.on('GameStateUpdate', (gs) => {
      state.roomId = gs.roomId;
      state.players = gs.players || [];
      state.turn = gs.turn;
      state.deckCount = gs.deckCount;
      state.discardCount = gs.discardCount;
      state.winnerId = gs.winnerId;

      if (gs.phase === 'Playing') {
        state.view = 'game';
      } else if (gs.phase === 'GameOver') {
        state.view = 'game';
      } else {
        state.view = 'waiting';
      }
    });

    connection.on('YourHand', (hand) => {
      state.hand = hand;
    });

    connection.on('GameOver', (winnerId) => {
      state.winnerId = winnerId;
      log('Ada pemenang!');
    });

    connection.onreconnecting(() => { state.connectionStatus = 'Menyambung ulang...'; });
    connection.onreconnected(() => { state.connectionStatus = 'Terhubung'; });
    connection.onclose(() => { state.connectionStatus = 'Terputus'; });

    connection.start()
      .then(() => {
        state.connectionStatus = 'Terhubung';
        state.myId = connection.connectionId;
        log('Terhubung ke server.');
        if (state.view === 'lobby') refreshRooms();
      })
      .catch((err) => {
        state.connectionStatus = 'Gagal terhubung';
        setError('Error: ' + err);
      });

    // Poll the room list while the player is in the lobby.
    let pollTimer = null;
    function startPolling() {
      stopPolling();
      pollTimer = setInterval(() => {
        if (state.view === 'lobby') refreshRooms();
      }, 3000);
    }
    function stopPolling() {
      if (pollTimer) {
        clearInterval(pollTimer);
        pollTimer = null;
      }
    }
    startPolling();

    return {
      ...toRefs(state),
      me,
      opponent,
      myOrgans,
      opponentOrgans,
      isMyTurn,
      canAct,
      canDraw,
      canSwap,
      swapCanConfirm,
      currentTurnName,
      winnerName,
      organImage,
      organLabel,
      cardEmoji,
      isTargetable,
      saveName,
      createRoom,
      joinRoom,
      joinKnownRoom,
      refreshRooms,
      startGame,
      drawCard,
      endTurn,
      clickCard,
      clickOrgan,
      toggleSwapMode,
      confirmSwap,
    };
  },
}).mount('#app');