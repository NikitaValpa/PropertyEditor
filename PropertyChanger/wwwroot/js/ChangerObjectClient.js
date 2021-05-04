let connection
function ChangerObjectClient() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/Editor")
        .withAutomaticReconnect()
        .build();

    connection.start({ transport: ['webSockets']}).then(() => {
        console.log("Соединение установлено " + connection.connectionId);
    }).catch(err => {
        console.error("Не получилось установить соединение " + err.toString())
    });
    

    connection.on("Recieve",(data) => {
        console.log(data);
    })

    this.SendObject = function () {
        connection.invoke("Edit", {}).catch(function (err) {
            return console.error(err.toString());//если не получилось, пишем в консоль браузера ошибку
        });
    }
}
document.addEventListener("DOMContentLoaded", () => {
    let client = new ChangerObjectClient();
})