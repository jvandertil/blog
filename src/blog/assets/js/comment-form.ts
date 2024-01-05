namespace CommentForm {
    export class CommentForm {
        private readonly form: HTMLFormElement;

        constructor(formId: string) {
            this.form = document.getElementById(formId)! as HTMLFormElement;

            this.form.addEventListener("submit", e => this.submitEventHandler(e));
        }

        private submitEventHandler(e: SubmitEvent) {
            e.stopPropagation();
            e.preventDefault();

            let submitBtn = e.submitter as HTMLButtonElement;
            let data = new FormData(this.form);

            submitBtn.disabled = true;

            for (let e of this.form.querySelectorAll('.is-invalid')) {
                e.classList.remove('is-invalid');
            }

            this.hideAlert('comment-success-alert');
            this.hideAlert('comment-failure-alert');
            this.hideAlert('comment-validation-alert');

            Utils.showElement(submitBtn.querySelector('.submitSpinner'));

            fetch(this.form.action, {
                method: 'post',
                body: data
            })
                .then(response => {
                    if (response.status === 200) {
                        this.form.reset();
                        this.showAlert('comment-success-alert');
                    } else if (response.status === 400) {
                        this.highlightErrors(response);
                        this.showAlert('comment-validation-alert');
                    } else {
                        // Some strange status, just show an error :(.
                        console.log("request failed with status: " + response.status);
                        this.showAlert('comment-failure-alert');
                    }
                })
                .catch(e => {
                    console.log("request failed with error: " + e);
                    this.showAlert('comment-failure-alert');
                })
                .finally(() => {
                    submitBtn.disabled = false;
                    Utils.hideElement(submitBtn.querySelector('.submitSpinner'));
                });
        }

        private highlightFields(response : any) {
            for (const field of document.getElementsByClassName('.form-field')) {
                field.classList.remove('is-invalid');
            }

            for (const val of response) {
                let propName = val.memberName;
                let nameSelector = '[name = "' + propName.replace(/(:|\.|\[|\])/g, "\\$1") + '"]',
                    idSelector = '#' + propName.replace(/(:|\.|\[|\])/g, "\\$1");

                let element = document.querySelector(nameSelector) || document.getElementById(idSelector);

                if (val.errorMessage.length > 0 && element != null) {
                    element.classList.add('is-invalid');
                }
            }
        };

        private highlightErrors(response: Response) {
            try {
                let data = response.json();
                data.then(e => this.highlightFields(e));
            } catch (e) {
                console.log("error deserializing json response.");
            }
        };

        private showAlert(alertId: string) {
            let alertElement = document.getElementById(alertId);

            if (alertElement != null) {
                Utils.showElement(alertElement);
                alertElement.scrollIntoView({ behavior: 'smooth' });
            }
        }

        private hideAlert(alertId: string) {
            let alertElement = document.getElementById(alertId);

            if (alertElement != null) {
                Utils.hideElement(alertElement);
            }
        }
    }

    export class CommentReplyForm {
        constructor(formId: string, toggleButtonId: string) {
            new CommentForm(formId);

            let toggler = document.getElementById(toggleButtonId) as HTMLButtonElement;
            toggler.addEventListener("click", () => this.toggleForm(formId))
        }

        private toggleForm(id: string) {
            var form = document.getElementById(id);
            if (Utils.isVisible(form)) {
                Utils.hideElement(form);
            } else {
                Utils.showElement(form);
            }
        }
    }
}

namespace Utils {
    export function isVisible(element: Element | null) {
        return element != null && !element.classList.contains("d-none");
    }

    export function hideElement(element: Element | null) {
        if (element != null) {
            element.classList.add("d-none");
            element.setAttribute("aria-hidden", "true")
        }
    }

    export function showElement(element: Element | null) {
        if (element != null) {
            element.classList.remove("d-none");
            element.setAttribute("aria-hidden", "false")
        }
    }
}
