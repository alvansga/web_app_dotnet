/**
 * CODENAME GAME - Centralized API Layer
 * Semua fetch call ke backend dikumpulkan di sini.
 */
const API_BASE_URL = '/api';

const api = {
    /* ── Player ── */
    async createPlayer(name) {
        const res = await fetch(`${API_BASE_URL}/players`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ name })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to create player');
        }
        return res.json();
    },

    /* ── Rooms ── */
    async createRoom() {
        const res = await fetch(`${API_BASE_URL}/rooms`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' }
        });
        if (!res.ok) throw new Error('Failed to create room');
        return res.json();
    },

    async getRooms() {
        const res = await fetch(`${API_BASE_URL}/rooms`);
        return res.json();
    },

    async getRoomDetail(code, playerId) {
        const url = `${API_BASE_URL}/rooms/${code}?playerId=${playerId || ''}`;
        const res = await fetch(url);
        if (!res.ok) return null;
        return res.json();
    },

    async joinRoom(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/join`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code, playerId })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Room not found or already started');
        }
    },

    async leaveRoom(playerId) {
        await fetch(`${API_BASE_URL}/rooms/leave`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId })
        });
    },

    async startGame(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to start game');
        }
        return res.json();
    },

    async assignSpymaster(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-spymaster`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async assignFieldOperative(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-field-operative`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async revealCard(code, cardId, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/reveal/${cardId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to reveal card');
        }
    },

    async addClue(code, playerId, word, count) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/clue`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ playerId, word, count })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to add clue');
        }
    },

    async removeClue(code, clueId, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/clue/${clueId}?playerId=${playerId}`, {
            method: 'DELETE'
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to remove clue');
        }
    }
};