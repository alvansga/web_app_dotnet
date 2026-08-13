# Changelog

## v1.0.0 — Vertical Slice MVP (Playable 2 Pemain)

Status: **PLAYABLE**. Logic game bisa dimainkan 2 orang/2 client secara end-to-end.

### Fitur yang sudah dibuat

**Game Engine (logika murni, server-authoritative)**
- 5 organ per pemain: Heart, Brain, Lungs, Liver, Kidneys.
- 4 tipe kartu inti: Affliction, Attack, Treatment, Defense.
- Deck: build (28 kartu), shuffle (Fisher-Yates, Random injectable), draw, discard, reshuffle dari discard.
- Turn manager: giliran bergantian + wrap, draw eksplisit sekali per giliran, batas 1 aksi per giliran.
- Card resolver: validasi target & efek per tipe kartu, termasuk mekanik shield (Defense memblokir serangan/affliction lalu habis).
- Win condition: semua organ lawan hancur → GameOver + winner tercatat.
- Validasi aturan penuh (bukan giliran, kartu bukan di tangan, target salah sisi/tipe, aksi setelah game over, dll.) → `GameRuleException`.

**SignalR Hub (API real-time)**
- Client → server: `CreateRoom`, `JoinRoom`, `StartGame`, `DrawCard`, `PlayCard`, `EndTurn`.
- Server → client: `PlayerJoined`, `GameStateUpdate` (state publik), `YourHand` (kartu privat per pemain), `GameOver`.
- Room manager in-memory (`RoomManager`) + map koneksi→room.
- Validasi giliran dilakukan di server; error aturan dikirim sebagai `HubException`.

**UI (Vue 3 via CDN, tanpa build step)**
- Lobby: isi nama, buat/gabung room (kode room).
- Waiting room: daftar pemain + tombol mulai.
- Game board: organ lawan & organ sendiri, deck/discard counter, indikator giliran, tombol Draw/End Turn.
- Hand kartu + interaksi klik: pilih kartu → highlight organ target valid → klik untuk main.
- Action log real-time.
- Semua organ & kartu dirender sebagai **teks/emoji placeholder** (belum gambar).

**Testing**
- 35 test xUnit, semua lulus (`dotnet test`).
- Unit test: Deck, Turn, CardResolver, GameEngine.
- Integration test `VerticalSliceTests`: alur penuh 2 pemain (buat/gabung room → start → draw → end turn → main kartu → menang).

### Cara menjalankan

```
dotnet run --project WebAppSandbox.csproj --launch-profile http
```

Buka `http://localhost:5253` di **dua tab/browser**. Satu tab "Buat Room", salin kode room, tab lain "Gabung Room", lalu mulai permainan.

### Catatan teknis
- Project utama & test target **.NET 10** (runtime di mesin: 6/8/10, tidak ada .NET 9).
- Folder `tests/**` dikecualikan dari kompilasi project web.

### Cara test

```
dotnet test tests\WebAppSandbox.Tests\WebAppSandbox.Tests.csproj
```
