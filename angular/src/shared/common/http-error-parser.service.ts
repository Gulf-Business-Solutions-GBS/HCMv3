import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

export interface ParsedHttpError {
    message: string;
    statusCode?: number;
    details?: string;
}

@Injectable({
    providedIn: 'root',
})
export class HttpErrorParserService {
    parseError(error: any, defaultMessage: string = 'An error occurred'): ParsedHttpError {
        const result: ParsedHttpError = {
            message: defaultMessage,
            statusCode: error?.status,
        };

        if (error instanceof HttpErrorResponse) {
            result.statusCode = error.status;

            if (error.error && typeof error.error === 'string') {
                const userFriendlyMessage = this.extractAbpUserFriendlyException(error.error);
                if (userFriendlyMessage) {
                    result.message = userFriendlyMessage;
                    result.details = error.error;
                    return result;
                }

                const firstLine = error.error.split(/[\r\n]+/)[0];
                if (firstLine && firstLine.trim()) {
                    result.message = firstLine.trim();
                    return result;
                }
            }

            if (error.error && typeof error.error === 'object') {
                if (error.error.error?.message) {
                    result.message = error.error.error.message;
                    result.details = error.error.error.details;
                    return result;
                }

                if (error.error.message) {
                    result.message = error.error.message;
                    return result;
                }

                if (error.error.error?.validationErrors && Array.isArray(error.error.error.validationErrors)) {
                    const validationMessages = error.error.error.validationErrors
                        .map((ve: any) => ve.message)
                        .filter((m: string) => m)
                        .join(', ');

                    if (validationMessages) {
                        result.message = validationMessages;
                        return result;
                    }
                }
            }

            if (error.statusText && error.statusText !== 'OK' && error.statusText !== 'Unknown Error') {
                result.message = error.statusText;
                return result;
            }

            return result;
        }

        if (error?.message) {
            result.message = error.message;
            return result;
        }

        return result;
    }

    getErrorMessage(error: any, defaultMessage: string = 'An error occurred'): string {
        return this.parseError(error, defaultMessage).message;
    }

    private extractAbpUserFriendlyException(errorString: string): string | null {
        const userFriendlyExceptionMatch = errorString.match(/Abp\.UI\.UserFriendlyException:\s*([^\r\n]+)/);
        if (userFriendlyExceptionMatch && userFriendlyExceptionMatch[1]) {
            return userFriendlyExceptionMatch[1].trim();
        }

        const simplifiedMatch = errorString.match(/UserFriendlyException:\s*([^\r\n]+)/);
        if (simplifiedMatch && simplifiedMatch[1]) {
            return simplifiedMatch[1].trim();
        }

        return null;
    }
}
