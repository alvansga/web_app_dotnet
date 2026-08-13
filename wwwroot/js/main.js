const { createApp, ref } = Vue;

createApp({
  setup() {
    const connectionStatus = ref('Menghubungkan...');
    const log = ref([]);
    let nextId = 1;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/game')
      .withAutomaticReconnect()
      .build();

    function addLog(message) {
      log.value.push({
        id: nextId++,
        time: new Date().toLocaleTimeString(),
        message,
      });
    }

    connection.on('Pong', (message, timestamp) => {
      addLog(`Pong diterima: "${message}" (${new Date(timestamp).toLocaleTimeString()})`);
    });

    connection.onreconnecting(() => {
      connectionStatus.value = 'Menyambung ulang...';
    });

    connection.onreconnected(() => {
      connectionStatus.value = 'Terhubung';
    });

    connection.onclose(() => {
      connectionStatus.value = 'Terputus';
    });

    connection
      .start()
      .then(() => {
        connectionStatus.value = 'Terhubung';
        addLog('Terhubung ke server SignalR');
      })
      .catch((err) => {
        connectionStatus.value = 'Gagal terhubung';
        addLog('Error: ' + err);
      });

    function ping() {
      connection.invoke('Ping', 'Halo dari client')
        .catch((err) => addLog('Ping gagal: ' + err));
    }

    function clearLog() {
      log.value = [];
    }

    return { connectionStatus, log, ping, clearLog };
  },
}).mount('#app');