import { Injectable, OnDestroy, inject } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { HubConnection } from '@microsoft/signalr';

@Injectable()
export class QrLoginSignalRService extends AppComponentBase implements OnDestroy {
    private sessionInterval: any;
    private sessionSetCount = 0;
    private maxSessionSetCount = 5;

    qrLoginHub: HubConnection;

    configureConnection(connection): void {
        this.qrLoginHub = connection;
        let reconnectTime = 5000;
        let tries = 1;
        let maxTries = 8;
        function start() {
            return new Promise(function (resolve, reject) {
                if (tries > maxTries) {
                    reject();
                } else {
                    connection
                        .start()
                        .then(resolve)
                        .then(() => {
                            reconnectTime = 5000;
                            tries = 1;
                        })
                        .catch(() => {
                            setTimeout(() => {
                                start().then(resolve);
                            }, reconnectTime);
                            reconnectTime *= 2;
                            tries += 1;
                        });
                }
            });
        }

        connection.onclose((e) => {
            if (e) {
                abp.log.debug('QR login SignalR connection closed with error: ' + e);
            } else {
                abp.log.debug('QR login SignalR disconnected');
            }

            start().then(() => {});
        });

        this.registerChatEvents(connection);
    }

    registerChatEvents(connection): void {
        connection.on('getAuthData', (message) => {
            abp.event.trigger('app.qrlogin.getAuthData', message);
        });
        connection.on('generateQrCode', (message) => {
            abp.event.trigger('app.qrlogin.generateQrCode', message);
        });
    }

    init(): Promise<void> {
        return new Promise((resolve, reject) => {
            try {
                abp.signalr.connect();
                abp.signalr
                    .startConnection(abp.appPath + 'signalr-qr-login', (connection) => {
                        this.configureConnection(connection);
                    })
                    .then(() => {
                        this.setSessionId();

                        this.sessionInterval = setInterval(() => {
                            if (this.sessionSetCount >= this.maxSessionSetCount) {
                                this.stopSessionInterval();
                            } else {
                                this.setSessionId();
                            }
                        }, 60000);

                        resolve();
                    })
                    .catch((error) => {
                        console.error('SignalR connection error:', error);
                        reject(error);
                    });
            } catch (error) {
                console.error('Error during SignalR connection:', error);
                reject(error);
            }
        });
    }

    setSessionId(): void {
        this.qrLoginHub.invoke('setSessionId');
        this.sessionSetCount++;
    }

    stopSessionInterval(): void {
        if (this.sessionInterval) {
            clearInterval(this.sessionInterval);
            this.sessionInterval = null;

            this.afterStopSessionInterval();
        }
    }

    afterStopSessionInterval(): void {
        abp.message.confirm(this.l('QrCodeExpiredMessage'), this.l('QrCodeExpiredTitle'), (isConfirmed: boolean) => {
            if (isConfirmed) {
                location.reload();
            }
        });
    }

    ngOnDestroy(): void {
        this.stopSessionInterval();
    }
}
