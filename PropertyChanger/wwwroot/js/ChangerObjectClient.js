function ClickSubmitButtonHandler() {
    let data = new FormData();
    data.append("NETObject", $(document.forms.changerPostForm.elements.NETObject).prop("files")[0]);
    $.ajax({
        url: "/Home/Desiarilizer", 
        type: "POST",
        data: data,
        processData: false,
        contentType: false,
        success: function (response) {
            console.log(response);
        },
        error: function (response) {
            console.error("Ошибка при отправке формы на сервер \n" + response);
        }
    });
}