import { Injectable } from '@angular/core';
import { RegisterTenantOutput } from '@shared/service-proxies/service-proxies';

@Injectable({ providedIn: 'root' })
export class TenantRegistrationHelperService {
    registrationResult: RegisterTenantOutput;
}
