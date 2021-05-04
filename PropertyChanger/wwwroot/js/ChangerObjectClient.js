let connection
function ChangerObjectClient() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/Editor")
        .withAutomaticReconnect()
        .build();

    connection.start({ transport:'webSockets'}).then(() => {
        console.log("Соединение установлено " + connection.connectionId);
    }).catch(err => {
        console.error("Не получилось установить соединение " + err.toString())
    });
    

    connection.on("Recieve", (data) => {
        console.log(data)
        let formElements = "";
        for (let key in data) {//заполняем нашу форму
            formElements += `<div class="row mt-4" id="PropRow">`    

            formElements += `<label for=${key} class="col-sm-2">${key}</label>`;
            if (typeof (data[key]) == "string") {//фильтруем намберы и стринги
                formElements += `<input type="text" class="form-control col-sm-2" value="${data[key]}">`;
            } else {
                formElements += `<input type="number" class="form-control col-sm-2" value="${data[key]}">`;
            }  
            formElements += `</div>` 
        }
        formElements += `<div class="row mt-4"><div class="col-auto"> <input type="button" id="sendToServer" value="Отправить на сервер"></div></div>`;//добавляем кнопку в самом конце
        $(document.forms.changerPostForm).html(formElements);

        $("input#sendToServer").click(() => {//вешаем на кнопку обработчик
            let rows = $("form#changerPostForm>div#PropRow");
            let dict = new Object();
            for (let i = 0; i < rows.length; i++) {
                let name = rows[i].children[0].innerText;
                let value;
                if (!isNaN(Number(rows[i].children[1].value))) {//фильтруем намберы и стринги, тоесть если не удаётся преобразовать в намбер, то значит это стринг
                    value = Number(rows[i].children[1].value);
                } else {
                    value = rows[i].children[1].value;
                }
                              
                dict[`${name}`] = value;
            }
            console.log(dict);
        })
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