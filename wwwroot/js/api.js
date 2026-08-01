/**
 * CODENAME GAME - Centralized API Layer
 * Semua fetch call ke backend dikumpulkan di sini.
 * Token dikirim di setiap request body untuk autentikasi.
 */
const API_BASE_URL = '/api';

/** Helper: baca CSRF token dari cookie */
function getCsrfToken() {
    const match = document.cookie.match(/(?:^|;\s*)CSRF-TOKEN=([^;]*)/);
    return match ? match[1] : '';
}

const api = {
    /* ── Player ── */
    async createPlayer(name) {
        const res = await fetch(`${API_BASE_URL}/players`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
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
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            }
        });
        if (!res.ok) throw new Error('Failed to create room');
        return res.json();
    },

    async getRooms() {
        const res = await fetch(`${API_BASE_URL}/rooms`);
        return res.json();
    },

    async getRoomDetail(code, playerId) {
        const token = store.player.token || '';
        const url = `${API_BASE_URL}/rooms/${code}?playerId=${playerId || ''}&token=${encodeURIComponent(token)}`;
        const res = await fetch(url);
        if (!res.ok) return null;
        return res.json();
    },

    async joinRoom(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/join`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ code, playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Room not found or already started');
        }
    },

    async leaveRoom(playerId) {
        await fetch(`${API_BASE_URL}/rooms/leave`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
    },

    async startGame(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/start`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to start game');
        }
        return res.json();
    },

    /* ── Team Role Assignment ── */
    async assignRedSpymaster(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-red-spymaster`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async assignBlueSpymaster(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-blue-spymaster`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async assignRedFieldOperative(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-red-operative`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async assignBlueFieldOperative(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-blue-operative`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    /* ── Legacy role assignment (kept for backward compat) ── */
    async assignSpymaster(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-spymaster`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async assignFieldOperative(code, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/assign-field-operative`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to assign role');
        }
    },

    async revealCard(code, cardId, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/reveal/${cardId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to reveal card');
        }
    },

    async addClue(code, playerId, word, count) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/clue`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-CSRF-TOKEN': getCsrfToken()
            },
            body: JSON.stringify({ playerId, word, count, token: store.player.token })
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to add clue');
        }
    },

    async removeClue(code, clueId, playerId) {
        const res = await fetch(`${API_BASE_URL}/rooms/${code}/clue/${clueId}?playerId=${playerId}&token=${encodeURIComponent(store.player.token)}`, {
            method: 'DELETE',
            headers: {
                'X-CSRF-TOKEN': getCsrfToken()
            }
        });
        if (!res.ok) {
            const txt = await res.text();
            throw new Error(txt || 'Failed to remove clue');
        }
    }
};