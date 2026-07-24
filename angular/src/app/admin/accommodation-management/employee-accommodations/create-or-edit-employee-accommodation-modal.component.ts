import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccommodationTypeDto,
    AccommodationTypesServiceProxy,
    BedDto,
    BedsServiceProxy,
    BuildingDto,
    BuildingsServiceProxy,
    CreateOrEditEmployeeAccommodationDto,
    EmployeeAccommodationsServiceProxy,
    EmployeeDto,
    EmployeesServiceProxy,
    FlatDto,
    FlatsServiceProxy,
    FloorDto,
    FloorsServiceProxy,
    GetAllAccommodationTypesInput,
    GetAllBedsInput,
    GetAllBuildingsInput,
    GetAllEmployeesInput,
    GetAllFlatsInput,
    GetAllFloorsInput,
    GetAllLocationsInput,
    GetAllRoomsInput,
    LocationDto,
    LocationsServiceProxy,
    RoomDto,
    RoomsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeAccommodationModal',
    templateUrl: './create-or-edit-employee-accommodation-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeAccommodationModalComponent extends AppComponentBase {
    private _service = inject(EmployeeAccommodationsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _accommodationTypesService = inject(AccommodationTypesServiceProxy);
    private _locationsService = inject(LocationsServiceProxy);
    private _buildingsService = inject(BuildingsServiceProxy);
    private _floorsService = inject(FloorsServiceProxy);
    private _flatsService = inject(FlatsServiceProxy);
    private _roomsService = inject(RoomsServiceProxy);
    private _bedsService = inject(BedsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeAccommodationDto>(new CreateOrEditEmployeeAccommodationDto());

    readonly employees = signal<EmployeeDto[]>([]);
    readonly accommodationTypes = signal<AccommodationTypeDto[]>([]);

    // NOTE: No established cascading-dropdown pattern was found elsewhere in this codebase
    // (e.g. no reusable "parent -> filtered child list" utility). This implementation loads
    // the full list for each facility level once and filters it client-side via computed()
    // signals based on the currently selected parent id, so selecting a Location filters the
    // Building dropdown, selecting a Building filters the Floor dropdown, and so on.
    readonly allLocations = signal<LocationDto[]>([]);
    readonly allBuildings = signal<BuildingDto[]>([]);
    readonly allFloors = signal<FloorDto[]>([]);
    readonly allFlats = signal<FlatDto[]>([]);
    readonly allRooms = signal<RoomDto[]>([]);
    readonly allBeds = signal<BedDto[]>([]);

    readonly buildings = computed(() => {
        const locationId = this.record().locationId;
        return locationId ? this.allBuildings().filter((b) => b.locationId === locationId) : this.allBuildings();
    });

    readonly floors = computed(() => {
        const buildingId = this.record().buildingId;
        return buildingId ? this.allFloors().filter((f) => f.buildingId === buildingId) : this.allFloors();
    });

    readonly flats = computed(() => {
        const floorId = this.record().floorId;
        return floorId ? this.allFlats().filter((f) => f.floorId === floorId) : this.allFlats();
    });

    readonly rooms = computed(() => {
        const flatId = this.record().flatId;
        return flatId ? this.allRooms().filter((r) => r.flatId === flatId) : this.allRooms();
    });

    readonly beds = computed(() => {
        const roomId = this.record().roomId;
        return roomId ? this.allBeds().filter((b) => b.roomId === roomId) : this.allBeds();
    });

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._accommodationTypesService
            .getAll(new GetAllAccommodationTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.accommodationTypes.set(result.items));

        this._locationsService
            .getAll(new GetAllLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.allLocations.set(result.items));

        this._buildingsService
            .getAll(new GetAllBuildingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, locationId: undefined }))
            .subscribe((result) => this.allBuildings.set(result.items));

        this._floorsService
            .getAll(new GetAllFloorsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, buildingId: undefined }))
            .subscribe((result) => this.allFloors.set(result.items));

        this._flatsService
            .getAll(new GetAllFlatsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, floorId: undefined }))
            .subscribe((result) => this.allFlats.set(result.items));

        this._roomsService
            .getAll(new GetAllRoomsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, flatId: undefined }))
            .subscribe((result) => this.allRooms.set(result.items));

        this._bedsService
            .getAll(new GetAllBedsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined, roomId: undefined }))
            .subscribe((result) => this.allBeds.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditEmployeeAccommodationDto());
            this.modal().show();
        } else {
            this._service.getEmployeeAccommodationForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAccommodation);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAccommodation_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof CreateOrEditEmployeeAccommodationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeAccommodationDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    // Cascading updates: clear dependent selections when a parent-level dropdown changes.
    updateLocation(value: number | undefined): void {
        const current = this.record();
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        next.locationId = value as any;
        next.buildingId = undefined as any;
        next.floorId = undefined as any;
        next.flatId = undefined as any;
        next.roomId = undefined as any;
        next.bedId = undefined as any;
        this.record.set(next);
    }

    updateBuilding(value: number | undefined): void {
        const current = this.record();
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        next.buildingId = value as any;
        next.floorId = undefined as any;
        next.flatId = undefined as any;
        next.roomId = undefined as any;
        next.bedId = undefined as any;
        this.record.set(next);
    }

    updateFloor(value: number | undefined): void {
        const current = this.record();
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        next.floorId = value as any;
        next.flatId = undefined as any;
        next.roomId = undefined as any;
        next.bedId = undefined as any;
        this.record.set(next);
    }

    updateFlat(value: number | undefined): void {
        const current = this.record();
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        next.flatId = value as any;
        next.roomId = undefined as any;
        next.bedId = undefined as any;
        this.record.set(next);
    }

    updateRoom(value: number | undefined): void {
        const current = this.record();
        const next = new CreateOrEditEmployeeAccommodationDto();
        Object.assign(next, current);
        next.roomId = value as any;
        next.bedId = undefined as any;
        this.record.set(next);
    }
}
