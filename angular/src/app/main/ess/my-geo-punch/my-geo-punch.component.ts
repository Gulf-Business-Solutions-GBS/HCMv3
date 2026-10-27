import {
    ChangeDetectionStrategy,
    Component,
    ElementRef,
    OnDestroy,
    OnInit,
    computed,
    inject,
    signal,
    viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppAuthService } from '@app/shared/common/auth/app-auth.service';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    CheckMyGeoPunchLocationInput,
    GeoPunchRangeVerdict,
    MyGeoPunchContextDto,
    MyGeoPunchDto,
    MyGeoPunchLocationCheckDto,
    MyGeoPunchServiceProxy,
    MyGeoPunchWorkLocationDto,
    RecordMyGeoPunchInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { finalize } from 'rxjs/operators';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';

/** The punch-type codes behind the four buttons; fixed, and the same ones the Time Engine reads. */
const PunchCode = { clockIn: '1', clockOut: '2', overtimeIn: '3', overtimeOut: '4' } as const;

interface Position {
    latitude: number;
    longitude: number;
    accuracy: number;
}

/**
 * Self-service geo punch: clock in or out from the browser, stamped with where the employee is standing.
 * Port of legacy ESS "Geo Punching".
 *
 * <p>Everything this page shows -- which buttons are open, how far away the work location is, whether that is
 * in range -- comes from the server, and the server decides it all again when the punch arrives. The page
 * only gathers the two things only a browser can: the GPS position and the selfie.</p>
 */
@Component({
    templateUrl: './my-geo-punch.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, HelpPageDirective],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyGeoPunchComponent extends AppComponentBase implements OnInit, OnDestroy {
    private readonly _service = inject(MyGeoPunchServiceProxy);
    private readonly _auth = inject(AppAuthService);

    readonly PunchCode = PunchCode;
    readonly Verdict = GeoPunchRangeVerdict;

    readonly loading = signal(true);
    readonly saving = signal(false);
    readonly notLinked = signal(false);
    readonly context = signal<MyGeoPunchContextDto | null>(null);

    readonly locating = signal(false);
    readonly locationError = signal<string | null>(null);
    readonly position = signal<Position | null>(null);
    readonly check = signal<MyGeoPunchLocationCheckDto | null>(null);

    readonly selectedWorkLocationId = signal<number | null>(null);

    readonly cameraError = signal<string | null>(null);
    readonly selfie = signal<string | null>(null);
    readonly selfiePreview = signal<string | null>(null);

    readonly now = signal(DateTime.local());

    private readonly _video = viewChild<ElementRef<HTMLVideoElement>>('video');
    private _stream: MediaStream | null = null;
    private _clockTimer: ReturnType<typeof setInterval> | undefined;
    /** Server time minus browser time, so the clock shows the time the punch will be stamped with. */
    private _clockOffsetMs = 0;

    readonly selectedWorkLocation = computed<MyGeoPunchWorkLocationDto | null>(() => {
        const id = this.selectedWorkLocationId();
        return this.context()?.workLocations?.find((w) => w.id === id) ?? null;
    });

    /** Punching is possible once there is a position and, unless open-location, a work location in range. */
    readonly readyToPunch = computed(() => {
        const ctx = this.context();
        if (!ctx || !ctx.hasDutyPeriodToday || !this.position() || this.saving()) {
            return false;
        }

        if (!ctx.isOpenLocation && !this.selectedWorkLocationId()) {
            return false;
        }

        return this.check()?.verdict !== GeoPunchRangeVerdict.OutOfRangeRefused;
    });

    ngOnInit(): void {
        this._clockTimer = setInterval(
            () => this.now.set(DateTime.local().plus({ milliseconds: this._clockOffsetMs })),
            1000
        );
        this.load(true);
    }

    override ngOnDestroy(): void {
        clearInterval(this._clockTimer);
        this.stopCamera();
        super.ngOnDestroy();
    }

    // ------------------------------------------------------------------ loading

    load(first = false): void {
        this.loading.set(true);

        this._service
            .getMyGeoPunchContext()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe({
                next: (ctx) => {
                    this.context.set(ctx);
                    this._clockOffsetMs = ctx.serverTime ? ctx.serverTime.toMillis() - Date.now() : 0;

                    // Keep the employee's choice across a reload if it is still offered; otherwise fall back to
                    // the default (their own work location), and to the only choice when there is just one.
                    const offered = ctx.workLocations ?? [];
                    const current = this.selectedWorkLocationId();
                    if (!offered.some((w) => w.id === current)) {
                        this.selectedWorkLocationId.set(
                            ctx.defaultWorkLocationId ?? (offered.length === 1 ? offered[0].id : null)
                        );
                    }

                    if (first) {
                        this.locate();
                        if (ctx.requiresSelfie) {
                            // The video element is rendered by the same change detection pass.
                            setTimeout(() => this.startCamera());
                        }
                    } else {
                        this.runCheck();
                    }
                },
                // Admin accounts land here: they are deliberately never linked to an employee.
                error: () => this.notLinked.set(true),
            });
    }

    // ------------------------------------------------------------------ location

    locate(onFound?: () => void, onFailed?: () => void): void {
        if (!navigator.geolocation) {
            this.locationError.set(this.l('GeoPunchLocationNotSupported'));
            onFailed?.();
            return;
        }

        this.locating.set(true);
        this.locationError.set(null);

        navigator.geolocation.getCurrentPosition(
            (pos) => {
                this.locating.set(false);
                this.position.set({
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude,
                    accuracy: pos.coords.accuracy,
                });
                this.runCheck(onFound, onFailed);
            },
            (error) => {
                this.locating.set(false);
                this.locationError.set(this.locationErrorText(error));
                this.message.warn(this.locationErrorText(error));
                onFailed?.();
            },
            { enableHighAccuracy: true, timeout: 20000, maximumAge: 0 }
        );
    }

    onWorkLocationChange(id: number | null): void {
        this.selectedWorkLocationId.set(id);
        this.runCheck();
    }

    private runCheck(onDone?: () => void, onFailed?: () => void): void {
        const ctx = this.context();
        const pos = this.position();
        if (!ctx || !pos) {
            onFailed?.();
            return;
        }

        if (!ctx.isOpenLocation && !this.selectedWorkLocationId()) {
            this.check.set(null);
            onFailed?.();
            return;
        }

        const input = new CheckMyGeoPunchLocationInput();
        input.latitude = pos.latitude;
        input.longitude = pos.longitude;
        input.workLocationId = ctx.isOpenLocation ? undefined : this.selectedWorkLocationId()!;

        this._service.checkMyGeoPunchLocation(input).subscribe({
            next: (result) => {
                this.check.set(result);
                if (result.verdict === GeoPunchRangeVerdict.OutOfRangeRefused) {
                    this.message.info(this.l('PunchingOutOfLocation'));
                    onFailed?.();
                    return;
                }
                onDone?.();
            },
            error: () => onFailed?.(),
        });
    }

    private locationErrorText(error: GeolocationPositionError): string {
        switch (error.code) {
            case error.PERMISSION_DENIED:
                return this.l('GeoPunchLocationPermissionDenied');
            case error.POSITION_UNAVAILABLE:
                return this.l('GeoPunchLocationUnavailable');
            case error.TIMEOUT:
                return this.l('GeoPunchLocationTimeout');
            default:
                return this.l('GeoPunchLocationUnavailable');
        }
    }

    // ------------------------------------------------------------------ camera

    startCamera(): void {
        this.cameraError.set(null);
        this.selfie.set(null);

        if (!navigator.mediaDevices?.getUserMedia) {
            this.cameraError.set(this.l('GeoPunchCameraUnavailable'));
            return;
        }

        navigator.mediaDevices
            .getUserMedia({ video: { facingMode: 'user' }, audio: false })
            .then((stream) => {
                this._stream = stream;
                const video = this._video()?.nativeElement;
                if (video) {
                    video.srcObject = stream;
                    void video.play();
                }
            })
            .catch(() => this.cameraError.set(this.l('GeoPunchCameraUnavailable')));
    }

    captureSelfie(): string | null {
        const video = this._video()?.nativeElement;
        if (!video || !video.videoWidth) {
            return null;
        }

        // Scaled down to at most 800 px wide: plenty to recognise a face, and a fraction of the upload.
        const scale = Math.min(1, 800 / video.videoWidth);
        const canvas = document.createElement('canvas');
        canvas.width = Math.round(video.videoWidth * scale);
        canvas.height = Math.round(video.videoHeight * scale);
        canvas.getContext('2d')!.drawImage(video, 0, 0, canvas.width, canvas.height);

        const dataUrl = canvas.toDataURL('image/jpeg', 0.8);
        this.selfie.set(dataUrl);
        this.stopCamera();

        return dataUrl;
    }

    retakeSelfie(): void {
        this.startCamera();
    }

    private stopCamera(): void {
        this._stream?.getTracks().forEach((t) => t.stop());
        this._stream = null;
    }

    // ------------------------------------------------------------------ punching

    punch(code: string): void {
        const ctx = this.context();
        if (!ctx || this.saving()) {
            return;
        }

        if (!ctx.isOpenLocation && !this.selectedWorkLocationId()) {
            this.message.warn(this.l('GeoPunchSelectWorkLocation'));
            return;
        }

        // Legacy took a snapshot on the button press when the employee had not taken one themselves.
        let selfie = this.selfie();
        if (ctx.requiresSelfie && !selfie) {
            selfie = this.captureSelfie();
            if (!selfie) {
                this.message.warn(this.l('GeoPunchSelfieRequired'));
                return;
            }
        }

        this.saving.set(true);

        // A fresh position for the punch itself, as legacy did: the one on screen may be minutes old. Any
        // failure on the way (no position, out of range) has already told the employee why.
        this.locate(
            () => this.submit(code, selfie),
            () => this.saving.set(false)
        );
    }

    private submit(code: string, selfie: string | null): void {
        const ctx = this.context()!;
        const pos = this.position()!;

        const input = new RecordMyGeoPunchInput();
        input.transTypeCode = code;
        input.latitude = pos.latitude;
        input.longitude = pos.longitude;
        input.workLocationId = ctx.isOpenLocation ? undefined : this.selectedWorkLocationId()!;
        input.selfie = ctx.requiresSelfie ? selfie! : undefined;

        this._service
            .recordMyGeoPunch(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => {
                this.notify.success(this.l('GeoPunchSuccessful'));

                if (result.autoLogout) {
                    setTimeout(() => this._auth.logout(true), 1200);
                    return;
                }

                if (ctx.requiresSelfie) {
                    this.retakeSelfie();
                }

                this.load();
            });
    }

    // ------------------------------------------------------------------ today's punches

    viewSelfie(row: MyGeoPunchDto): void {
        this._service.getMyGeoPunchSelfie(row.id).subscribe((result) => this.selfiePreview.set(result.dataUrl ?? null));
    }

    closeSelfie(): void {
        this.selfiePreview.set(null);
    }

    // ------------------------------------------------------------------ display helpers

    typeName(row: MyGeoPunchDto): string {
        return this.pick(row.transTypeNameEn, row.transTypeNameAr);
    }

    workLocationName(w: { nameEn?: string; nameAr?: string } | null | undefined): string {
        return w ? this.pick(w.nameEn, w.nameAr) : '';
    }

    rowWorkLocationName(row: MyGeoPunchDto): string {
        return this.pick(row.workLocationNameEn, row.workLocationNameAr);
    }

    verdictLabel(verdict: GeoPunchRangeVerdict | undefined): string {
        switch (verdict) {
            case GeoPunchRangeVerdict.InRange:
                return this.l('GeoPunchInRange');
            case GeoPunchRangeVerdict.OutOfRangeAllowed:
                return this.l('GeoPunchOutOfRangeAllowed');
            default:
                return this.l('GeoPunchOutOfRange');
        }
    }

    verdictClass(verdict: GeoPunchRangeVerdict | undefined): string {
        switch (verdict) {
            case GeoPunchRangeVerdict.InRange:
                return 'badge-success';
            case GeoPunchRangeVerdict.OutOfRangeAllowed:
                return 'badge-warning';
            default:
                return 'badge-danger';
        }
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }

    private pick(en: string | undefined, ar: string | undefined): string {
        return this.isRtl ? ar || en || '' : en || '';
    }
}
