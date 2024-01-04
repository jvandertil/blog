function isVisible(element) {
    return !element.classList.contains("d-none");
}

function hideElement(element) {
    element.classList.add("d-none");
    element.setAttribute("aria-hidden", "true")
}

function showElement(element) {
    element.classList.remove("d-none");
    element.setAttribute("aria-hidden", "false")
}

function highlightFields(response) {
    for (const field of document.getElementsByClassName('.form-field')) {
        field.classList.remove('is-invalid');
    }

    for (const val of response) {
        let propName = val.memberName;
        let nameSelector = '[name = "' + propName.replace(/(:|\.|\[|\])/g, "\\$1") + '"]',
            idSelector = '#' + propName.replace(/(:|\.|\[|\])/g, "\\$1");

        let element = document.querySelector(nameSelector) || document.getElementById(idSelector);

        if (val.errorMessage.length > 0) {
            element.classList.add('is-invalid');
        }
    }
};

function highlightErrors(response) {
    try {
        let data = response.json();
        data.then(e => highlightFields(e));
    } catch (e) {
        console.log("error deserializing json response.");
    }
};

function showAlert(alertId) {
    let alertElement = document.getElementById(alertId);

    showElement(alertElement);
    alertElement.scrollIntoView({ behavior: 'smooth' });
}

function hideAlert(alertId) {
    let alertElement = document.getElementById(alertId);
    hideElement(alertElement);
}

for (const f of document.getElementsByTagName("form")) {
    if (f.method !== "post" || f.classList.contains("no-ajax")) {
        continue;
    }

    f.addEventListener("submit", e => {
        e.stopPropagation();
        e.preventDefault();

        let submitBtn = e.submitter;
        let form = e.target;
        let data = new FormData(form);

        submitBtn.disabled = true;

        for (let e of form.querySelectorAll('.is-invalid')) {
            e.classList.remove('is-invalid');
        }

        hideAlert('comment-success-alert');
        hideAlert('comment-failure-alert');
        hideAlert('comment-validation-alert');

        showElement(submitBtn.querySelector('.submitSpinner'));

        fetch(form.action, {
            method: 'post',
            body: data
        })
            .then(response => {
                if (response.status === 200) {
                    form.reset();
                    showAlert('comment-success-alert');
                } else if (response.status === 400) {
                    highlightErrors(response);
                    showAlert('comment-validation-alert');
                } else {
                    // Generic error message.
                    console.log("request failed with status: " + response.status);
                    showAlert('comment-failure-alert');
                }
            })
            .catch(e => {
                console.log("request failed with error: " + e);
                showAlert('comment-failure-alert');
            })
            .finally(() => {
                submitBtn.disabled = false;
                hideElement(submitBtn.querySelector('.submitSpinner'));
            });
    })
}

function toggleForm(id) {
    var form = document.getElementById(id);
    if (isVisible(form)) {
        hideElement(form);
    } else {
        showElement(form);
    }
}
