document.title = 'Gastos WebApi';

function getBearer() {
    var ret = null;
    var bearer = null;

    if (bearer == null && typeof(localStorage.authorized) != 'undefined')
        bearer = JSON.parse(localStorage.authorized);

    if (bearer == null && ui != null && ui.authSelectors != null) {
        var auth = ui.authSelectors.authorized();
        if (auth != null && auth.toJS() != null)
            bearer = auth.toJS();
    }

    if (bearer != null && typeof(bearer.Bearer) != 'undefined')
        ret = bearer.Bearer.value;

    return ret;
}

function getAuthorized(url) {
    console.log('getAuthorized: ' + url);

    var headers = null;
    var auth = getBearer();
    var hasButtonAuthorize = $(".btn.authorize").length > 0;

    if (auth != null && hasButtonAuthorize)
        headers = {
            "Authorization": auth
        };

    $.ajax({
        type: "GET"
        , url: url
        , dataType: 'json'
        , headers: headers
        , fail: function(jqXHR, textStatus ) {
            alertXhr(jqxHR, textStatus);
        }
        , statusCode: {
            200: function(data) {
                console.log('200: ' + data);

                var ret = data;
                var json = JSON.stringify(ret, null, 2);
                // var newwindow = window.open('', '_blank');
                // if (newwindow && newwindow != null) {
                //     var html = 
                //     '<html>'
                //     + '<head><meta name="color-scheme" content="light dark">'
                //     + '</head>'
                //     + '<body><pre style="word-wrap: break-word; white-space: pre-wrap;">'
                //     + '[retValue]'
                //     + '</pre>'
                //     + '</body>'
                //     + '</html>';
                //     html = html.replace('[retValue]', json);
                    
                //     newwindow.document.body.innerHTML = html;
                // } else {
                    // alert ("Can't open a new window.");
                    $("#wndResultBody").html(json);
                    $("#wndResult")[0].showModal();
                // }
            }
            , 400: alertXhr
            , 401: alertXhr
            , 500: alertXhr
        }
    });
}

function alertXhr(jqXHR, textStatus) {
    console.log('textStatus:' + textStatus);
    console.log('jqXHR:');
    console.log(jqXHR);

    var errMsg = null;

    if (errMsg == null && jqXHR != null && jqXHR.responseText != null && jqXHR.responseText != '')
        errMsg = jqXHR.responseText;
    if (errMsg == null && jqXHR != null && jqXHR.statusText != null && jqXHR.statusText != '')
        errMsg = jqXHR.statusText;
    if (errMsg == null && textStatus != null && textStatus != '')
        errMsg = textStatus;

    if (errMsg == null)
        alert("Undetermined response");
    else {
        if (jqXHR != null && jqXHR.status != null)
            alert( "Request failed [" + jqXHR.status + "]: " + errMsg);
        else
            alert(errMsg);
    }
}

function transformLinks() {
    console.log("transformLinks: " + $('a[href^="[doclink]"]').length + " links.");

    $('a[href^="[doclink]"]')
        .addClass("doclink")
        .attr("href", "javascript:void(0);")
        .on("click", function() {
            getAuthorized($(this).text());
        });
}

$(document).ready(function() {
    console.log("ready: " + $(".opblock-summary-control").length + " buttons.");

    // From: https://stackoverflow.com/questions/61077579/how-to-load-javascript-files-after-swagger-ui-is-loaded-in-net-core-web-api-pro
    var checkButtonsExist = setInterval(function () {
        if ($(".opblock-summary-control").length > 0) {
            clearInterval(checkButtonsExist);
    
            console.log("setInterval: " + $(".opblock-summary-control").length + " buttons.");

            // Add an "onClick" event listener to the method's buttons.
            $(".opblock-summary-control").on("click", function() {
                var num = $(".opblock-body").length;
                console.log("opblock-body: " + num + " descriptions.");

                var checkDescriptionExists = setInterval(function () {
                    if ($(".opblock-body").length != num) {
                        clearInterval(checkDescriptionExists);

                        console.log("setInterval2: " + $(".opblock-body").length + " descriptions.");

                        transformLinks();
                    }
                }, 100);
            });

            // Create a dialog to show the results.
            var dialog = $('<dialog id="wndResult" class="code-dialog">'
                + '<div>'
                + '<button class="close-dialog-button" autofocus>'
                + '<span class="close-dialog-span">Close</span>'
                + '</button>'
                + '</div>'
                + '<div class="dialog-body">'
                + '<pre class="microlight" style="display: block; overflow-x: auto; padding: 0.5em; background: rgb(51, 51, 51); color: white;">'
                + '<code id="wndResultBody" class="language-json" style="white-space: pre;">'
                + '</code>'
                + '</pre>'
                + '</div>'
                + '</dialog>');
            dialog.appendTo('body');

            // Close the dialog when button clicked.
            $("button.close-dialog-button").on("click", function() { 
                $(this).closest("dialog")[0].close(); 
            });

            // Open the dialog with "$("#wndResult")[0].showModal()".
        }
    }, 100); // check every 100ms
});
