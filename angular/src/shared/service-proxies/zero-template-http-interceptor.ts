import { Injectable, inject } from '@angular/core';
import { AbpHttpInterceptor, MessageService } from 'abp-ng2-module';
import { HttpResponse, HttpErrorResponse } from '@angular/common/http';
import { Observable, switchMap, throwError } from 'rxjs';
import { HttpErrorParserService } from '@shared/common/http-error-parser.service';

@Injectable({
    providedIn: 'root',
})
export class ZeroTemplateHttpInterceptor extends AbpHttpInterceptor {
    private messageService: MessageService = new MessageService();
    private httpErrorParser = inject(HttpErrorParserService);

    protected handleErrorResponse(error: any): Observable<never> {
        this.addParsedMessageToError(error);

        if (!(error.error instanceof Blob)) {
            this.messageService.error('Server is unreachable');
            return throwError(() => error);
        }

        return this.configuration.blobToText(error.error).pipe(
            switchMap((json) => {
                const errorBody = json === '' || json === null ? {} : JSON.parse(json);
                const errorResponse = new HttpResponse({
                    headers: error.headers,
                    status: error.status,
                    body: errorBody,
                });

                let ajaxResponse = this.configuration.getAbpAjaxResponseOrNull(errorResponse);

                if (ajaxResponse != null) {
                    this.configuration.handleAbpResponse(errorResponse, ajaxResponse);
                } else {
                    this.configuration.handleNonAbpErrorResponse(errorResponse);
                }

                return throwError(() => error);
            })
        );
    }

    private addParsedMessageToError(error: any): void {
        if (!(error instanceof HttpErrorResponse)) {
            return;
        }

        const parsedError = this.httpErrorParser.parseError(error, 'An error occurred');

        if (typeof error.error === 'object' && error.error !== null && !(error.error instanceof Blob)) {
            if (!error.error.message) {
                error.error.message = parsedError.message;
            }
        } else if (typeof error.error === 'string') {
            const originalError = error.error;
            (error as any).error = {
                originalError: originalError,
                message: parsedError.message,
            };
        } else if (!error.error || error.error === null) {
            (error as any).error = {
                message: parsedError.message,
            };
        }
    }
}
