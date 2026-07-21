import { Pipe, PipeTransform, inject } from '@angular/core';
import { FeatureCheckerService } from 'abp-ng2-module';

@Pipe({ name: 'checkFeature' })
export class FeatureCheckerPipe implements PipeTransform {
    private featureCheckerService = inject(FeatureCheckerService);

    transform(feature: string): boolean {
        return this.featureCheckerService.isEnabled(feature);
    }
}
