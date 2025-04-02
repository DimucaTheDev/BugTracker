plfI = false;
function post_login_form() {
    //if (plfI) return;
    plfI = true;
    document.getElementById('login_form').addEventListener('submit', async function (event) {
        document.getElementById("login_error").innerText = "";
        event.preventDefault();
        const form = event.target;
        const formData = new FormData(form);
        try {
            const response = await fetch(form.action, {
                method: form.method,
                body: formData
            });
            if (response.status === 400) {
                const data = await response.json();
                document.getElementById("login_error").innerText = data.description;
                //alert(data.description); // Показываем сообщение из description
            } else if (response.status === 200) {
                document.getElementById("login_error").style.color = "green";
                document.getElementById("login_error").innerText = "Успех!";
                
                setTimeout(async function () {
                    const data = await response.json();
                    if (typeof(data.redir) == "string") {
                        var object = data.redir;
                        location.replace(object.startsWith("/") ? (location.origin + object) : object);
                        return;
                    }

                    var args = new URLSearchParams(window.location.search);
                    if (args.has("redir")) {
                        var object = args.get("redir");
                        // ?redir=/Page                ->  http://localhost:X/Page
                        // ?redir=https://example.com  ->  https://example.com
                        // ?redir=example.com          ->  example.com //TODO: add https:// 
                        location.replace(object.startsWith("/") ? (location.origin + object) : object);
                    } else
                        location.reload();
                }, 500);
            } else if (response.status === 500) {
                document.getElementById("login_error").style.color = "green";
                document.getElementById("login_error").innerText = "Успех!";
            }
        } catch (error) { console.error('Ошибка запроса:', error); }
    });
}
window.addEventListener("click", (event) => {
    let dropdown = document.querySelector(".dropdown-content");
    if (dropdown && !event.target.closest(".dropdown")) {
        dropdown.style.display = "none";
    }
});

 
