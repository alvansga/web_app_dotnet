# ROADMAP — Web App Card Game "Organ Attack"

Web aplikasi card game bertema **Organ Attack** (adaptasi dari OrganATTACK!). Server menjadi sumber kebenaran tunggal; dua client bisa bermain 1v1 secara real-time.

---

## 1. Keputusan Teknis

| Aspek | Pilihan |
|---|---|
| Backend | ASP.NET Core (.NET 9) + **SignalR** (server otoritatif) |
| Frontend | **Vue 3 (JavaScript, Composition API)** via CDN/ES modules, tanpa build step |
| Penyajian UI | Static files di `wwwroot/`, diserve langsung oleh ASP.NET Core |
| Aturan | OrganATTACK! versi asli, mulai dari **MVP lalu expand bertahap** |
| Visual | **Teks/emoji placeholder** dulu (kartu & organ), aset gambar menyusul |
| Testing | xUnit untuk unit-test Game Engine |

---

## 2. Prinsip Arsitektur

1. **Server = satu-satunya sumber kebenaran (authoritative).**
   - Semua state game & validasi aturan berada di server.
   - Client hanya render state reaktif dan mengirim **intent** (misal `PlayCard(cardId, targetOrganId)`).

2. **Game Engine murni.**
   - Logika game TIDAK bergantung pada SignalR/HTTP.
   - Berguna agar logic bisa di-*unit test* tanpa menjalankan server/client.
   - SignalR hanya bertindak sebagai "jembatan" transport.

3. **State machine game.**
   - `WaitingForPlayer → Playing → GameOver`.

---

## 3. Struktur File

```
WebAppSandbox/
├── Program.cs                  # bootstrap, static files, map SignalR hub
├── GameEngine/                 # LOGIKA MURNI (prioritas utama)
│   ├── Models/                 # Card, Organ, Player, Game, Deck, TurnState, enums
│   ├── Rules/                  # GameEngine, TurnManager, CardResolver
│   └── Cards/                  # data kartu (affliction/attack/treatment/defense)
├── Hubs/
│   └── GameHub.cs              # entry point 2 client
├── wwwroot/                    # frontend Vue (tanpa build step)
│   ├── index.html
│   ├── css/style.css
│   ├── js/
│   │   ├── main.js             # bootstrap Vue + SignalR client
│   │   ├── store.js            # reactive game state (reactive/ref)
│   │   └── components/
│   │       ├── BoardView.vue   # atau .js + template
│   │       ├── OrganSlot.vue
│   │       ├── HandView.vue
│   │       ├── CardView.vue    # render teks, bukan gambar
│   │       └── TurnIndicator.vue
│   └── assets/
│       └── CHANGELOG.md        # daftar placeholder gambar -> teks
└── tests/                      # unit test GameEngine (xUnit)
```

> Karena dipilih tanpa build step, komponen Vue ditulis sebagai file `.js`/template string atau single-file sederhana yang di-load dari CDN. Jika nanti ingin struktur SFC (`*.vue`) + hot-reload, tinggal tambah Vite.

---

## 4. Domain Model (Entity & Enum)

### Enum

```
CardType      = Affliction | Attack | Treatment | Defense | Special
OrganType     = Heart | Brain | Lungs | Liver | Kidneys
GamePhase     = WaitingForPlayer | Playing | GameOver
TargetType    = SelfOrgan | OpponentOrgan | NoTarget
```

### Class utama

```
Card
  - Id
  - Name
  - CardType
  - TargetOrganType (nullable, untuk kartu yang menyasar organ tertentu)
  - Effect (deskripsi/behavior)

Organ
  - OrganType
  - IsDestroyed (bool)
  - IsAfflicted (bool / daftar affliction)
  - Position (untuk layout board)

Player
  - ConnectionId / PlayerId
  - Name
  - Organs: List<Organ>
  - Hand: List<Card>
  - IsAlive => ada organ yang belum destroyed

Game
  - Id
  - Phase
  - Players: List<Player>
  - Deck
  - DiscardPile
  - CurrentPlayerIndex
  - WinnerPlayerId (nullable)

Deck
  - Cards: List<Card>
  - Build(), Shuffle(), Draw()

TurnState
  - PlayerId (pemain giliran)
  - ActionsLeft / batas aksi per giliran
```

---

## 5. Aturan Game (MVP Terlebih Dahulu)

### Organ (per player)
- **Heart**, **Brain**, **Lungs**, **Liver**, **Kidneys**.
- Organ bisa kondisi: **sehat**, **teraffliction**, **destroyed**.

### Tipe Kartu Inti
| Tipe | Fungsi |
|---|---|
| **Affliction** | Menandai organ lawan sebagai "teraffliction" |
| **Attack** | Menghancurkan organ lawan (biasanya hanya ke organ yang teraffliction & tipe cocok) |
| **Treatment** | Menyembuhkan affliction / memulihkan organ sendiri |
| **Defense** | Menangkal serangan / melindungi organ |

### Alur Turn (MVP)
1. Pemain menarik kartu (draw).
2. Pemain boleh memainkan kartu dari tangan (validasi target & tipe).
3. Pemain mengakhiri giliran (end turn).
4. Giliran berpindah ke lawan.

### Win Condition
- Semua organ lawan `destroyed` → pemain lawan kalah.

### Catatan Aturan Asli (Fase 5)
- Mekanik **"catch"** (lawan bisa menangkap kartu).
- Kartu **special/wild**.
- Pengocokan posisi organ.
- Detail aturan asli diverifikasi ke rulebook resmi, di-encode bertahap.

---

## 6. API SignalR (GameHub)

### Client → Server (invoke)
| Method | Deskripsi |
|---|---|
| `CreateRoom()` | Buat room baru |
| `JoinRoom(roomId)` | Gabung ke room |
| `StartGame()` | Mulai permainan (jika 2 pemain siap) |
| `DrawCard()` | Ambil kartu dari deck |
| `PlayCard(cardId, targetOrganId?)` | Mainkan kartu ke target |
| `EndTurn()` | Akhiri giliran |

### Server → Client (broadcast/send)
| Method | Deskripsi |
|---|---|
| `GameStateUpdate(gameState)` | Kirim state penuh/reaktif ke client |
| `PlayerJoined(playerInfo)` | Notifikasi pemain baru |
| `GameOver(winnerId)` | Notifikasi akhir game |
| `ActionLog(message)` | Log aksi terbaru |

Validasi penting: **hanya pemain yang sedang giliran** yang boleh memanggil aksi.

---

## 7. Fase Roadmap

### Fase 0 — Fondasi Jaringan (verifikasi 2 client)
- Serve static files `wwwroot/`, setup SignalR hub.
- Vue root app + SignalR client connect ke room yang sama.
- Deliverable: 2 tab browser terhubung & bisa saling `Ping`.

### Fase 1 — Game Engine Murni (LOGIC UTAMA, bedah terbanyak)
- Model 5 organ per player.
- 4 tipe kartu inti.
- Deck: build, shuffle, draw, discard pile, hand.
- `TurnManager` (giliran & batas aksi), `CardResolver` (validasi & apply efek).
- Win condition: semua organ lawan hancur.
- **Deliverable:** bisa main penuh via console/unit test tanpa UI.

### Fase 2 — API SignalR + Room Management
- Implement method hub `CreateRoom`, `JoinRoom`, `StartGame`, `PlayCard`, `DrawCard`, `EndTurn`.
- Broadcast `GameStateUpdate` ke 2 client.
- Validasi giliran di server.

### Fase 3 — UI Vue
- Komponen: board organ (saya vs lawan), hand, deck, discard, indikator giliran, action log.
- Interaksi klik/drag untuk main kartu.
- Semua kartu & organ dirender **teks/emoji** (bukan gambar).

### Fase 4 — Vertical Slice MVP
- 2 client main penuh aturan inti end-to-end.
- Milestone: **"logic gamenya bisa dimainkan 2 orang".**

### Fase 5 — Expand Aturan Asli
- Mekanik "catch", kartu special/wild, pengocokan posisi organ, efek tambahan.
- Detail aturan asli diverifikasi ke rulebook resmi, di-encode bertahap.

### Fase 6 — Polish
- Reconnect & penanganan disconnect.
- Timer giliran.
- Animasi/sound.
- Ganti placeholder teks → aset gambar.
- Riwayat game.

---

## 8. Prioritas Eksekusi

1. **Fase 1 (Game Engine murni)** = paling penting; semua logic game + unit test di sini.
2. **Fase 0 + 2** = koneksikan 2 client via SignalR.
3. **Fase 3 + 4** = bungkus dengan Vue jadi playable.
4. **Fase 5 + 6** = lengkapi aturan & rapikan.

---

## 9. Placeholder Visual (Teks → Gambar)

Semua aset visual awal memakai **teks/emoji**. Berikut daftar penggantinya nanti:

| Elemen | Placeholder Awal | Aset Nanti |
|---|---|---|
| Kartu | Judul + deskripsi teks | Ilustrasi kartu |
| Organ (Heart) | ❤️ Heart | Ilustrasi jantung |
| Organ (Brain) | 🧠 Brain | Ilustrasi otak |
| Organ (Lungs) | 🫁 Lungs | Ilustrasi paru-paru |
| Organ (Liver) | 🩸 Liver | Ilustrasi hati |
| Organ (Kidneys) | 🫘 Kidneys | Ilustrasi ginjal |
| Papan board | Blok kartu berlabel | Layout board bergambar |

Asset path dipisahkan ke dalam `wwwroot/assets/` agar tinggal ganti referensi gambar tanpa mengubah logic.

---

## 10. Definisi Selesai (Definition of Done)

- Game Engine punya unit test lengkap & hijau.
- 2 client berbeda bisa connect, join room, dan menyelesaikan satu game penuh (MVP).
- Server menolak aksi yang melanggar aturan (misal memainkan kartu saat bukan gilirannya).
- Semua visual masih placeholder teks, namun interaksi & logic sudah berjalan.