function ChangerObjectClient() {
    let connection = new signalR.HubConnectionBuilder()
        .withUrl("/Editor")
        .withAutomaticReconnect()
        .build();

    connection.start({ transport:'webSockets'}).then(() => {
        console.log("Соединение установлено " + connection.connectionId);
    }).catch(err => {
        console.error("Не получилось установить соединение " + err.toString())
    });

    connection.on("Error", (errorTitle, errorMessage) => {
        console.error("Ошибка " + errorMessage);
        $(document.forms.changerPostForm).html("");//чистим нашу форму перед новой отрисовкой

        let formElements = "";
        
        formElements += `<div class="row mt-4">`
        formElements += `<div class="col-3"><h5 style="color:red; text-align:left">${errorTitle}</h5></div>`
        formElements += `</div>`
        formElements += `<div class="row mt-2">`
        formElements += `<div class="col-auto"><p style="color:red">${errorMessage}</p></div>`
        formElements += `</div>`
        
        $(document.forms.changerPostForm).html(formElements);
    })

    connection.on("Recieve", (data) => {
        console.log("Объект пришедший от сервера ")
        console.log(data);

        $(document.forms.changerPostForm).html("");//чистим нашу форму перед новой отрисовкой

        let formElements = "";
        for (let key in data) {//заполняем нашу форму
            if (key == "Edited") {//нам не надо отрисовывать этот флаг в форме, если он есть
                continue;
            }
            formElements += `<div class="row mt-4" id="PropRow">`    

            formElements += `<label for=${key} class="col-sm-2">${key}</label>`;
            if (typeof (data[key]) == "string") {//фильтруем намберы и стринги
                formElements += `<input type="text" class="form-control col-sm-2" value="${data[key]}">`;
            } else {
                formElements += `<input type="number" class="form-control col-sm-2" value="${data[key]}">`;
            }
            formElements += `</div>` 
        }
        if (data["Edited"]) {//проверяем наличие флага в присланном объекте, если он есть, то значит сигнализируем, что объект изменён
            formElements += `<div class="row mt-4"><div class="col-auto"> <input type="button" id="sendToServer" value="Изменить объект"><p style="color:green">${data["Edited"]==true?"Объект успешно изменён":""}</p></div></div>`;//добавляем кнопку в самом конце
        } else {
            formElements += `<div class="row mt-4"><div class="col-auto"> <input type="button" id="sendToServer" value="Изменить объект"></div></div>`;//добавляем кнопку в самом конце
        }
        
        $(document.forms.changerPostForm).html(formElements);

        $('input[type="number"]').on('change keyup', function () {//так как дурацкие браузеры позволяют писать буквы в инпутах с типом намбер, вешаем реплейсеры
            // Remove invalid characters
            var sanitized = $(this).val().replace(/[^-0-9]/g, '');
            // Remove non-leading minus signs
            sanitized = sanitized.replace(/(.)-+/g, '$1');
            // Update value
            $(this).val(sanitized);
        });


        $("input#sendToServer").click(() => {//вешаем на кнопку обработчик
            let rows = $("form#changerPostForm>div#PropRow");
            let dict = new Object();
            for (let i = 0; i < rows.length; i++) {
                let name = rows[i].children[0].innerText;
                let value;
                if (!isNaN(Number(rows[i].children[1].value)) && Number(rows[i].children[1].value!="")) {//фильтруем намберы и стринги, тоесть если не удаётся преобразовать в намбер, то значит это стринг
                    value = Number(rows[i].children[1].value);
                } else {
                    value = rows[i].children[1].value.toString();
                }
                              
                dict[`${name}`] = value;
            }
            console.log("Объект отправляемый серверу ");
            console.log(dict);

            connection.invoke("Recieve", dict).catch(err => {
                console.error("Не получилось отправить изменённый объект на сервер по причине " + err.toString());
            })
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