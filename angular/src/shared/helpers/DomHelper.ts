export class DomHelper {
    static waitUntilElementIsReady(selector: string, callback: any, checkPeriod?: number): void {
        let selectors = selector.split(',');
        let elementCount = selectors.length;

        if (!checkPeriod) {
            checkPeriod = 100;
        }

        let checkExist = setInterval(() => {
            let foundElementCount = 0;
            for (let i = 0; i < selectors.length; i++) {
                let selector = selectors[i].trim();
                if (selector[0] === '#') {
                    let idSelector = selector.replace('#', '');
                    foundElementCount = foundElementCount + (document.getElementById(idSelector) ? 1 : 0);
                } else if (selector[0] === '.') {
                    let classSelector = selector.replace('.', '');
                    foundElementCount = foundElementCount + (document.getElementsByClassName(classSelector) ? 1 : 0);
                }
            }

            if (foundElementCount >= elementCount) {
                clearInterval(checkExist);
                callback();
            }
        }, checkPeriod);
    }

    static createElement(tag: string, attributes: any[]): any {
        let el = document.createElement(tag);

        const allowedAttributes = ['id', 'rel', 'href'];

        for (let i = 0; i < attributes.length; i++) {
            let attribute = attributes[i];

            if (allowedAttributes.includes(attribute.key)) {
                // href ise encodeURI kullan
                if (attribute.key === 'href') {
                    attribute.value = encodeURI(attribute.value);
                }

                el.setAttribute(attribute.key, attribute.value);
            }
        }

        return el;
    }

    static getElementByAttributeValue(tag: string, attribute: string, value: string) {
        let els = document.getElementsByTagName(tag);
        if (!els) {
            return undefined;
        }

        for (let i = 0; i < els.length; i++) {
            let el = els[i];
            if (el.getAttribute(attribute) === value) {
                return el;
            }
        }

        return undefined;
    }
}
