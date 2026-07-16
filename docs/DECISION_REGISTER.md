# Decision Register

| D-27 | Initial Album membership | Optional `memoryIds` in one create request | Prevents partial Album relationships | Privacy/data integrity | Phase 19E Checkpoint 2C | APPROVED | Atomic backend contract |
| D-28 | Album editing scope | Name and optional description only | Keep relationship/delete decisions for Checkpoint 2D | Product sequencing | Phase 19E Checkpoint 2C | APPROVED | Implemented |

Mọi dòng dưới đây chưa được phê duyệt bởi chủ sở hữu dự án. Recommendation chỉ để chuẩn bị thảo luận, không phải quyết định triển khai.

| ID | Topic | Current state / options | Recommendation | Impact | Required before phase | Status | User decision |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D-01 | Production database | LocalDB dev; SQLite test; production unknown | Managed SQL Server nếu vận hành nhỏ | Hosting/secrets/backup | Production deploy | OPEN — REQUIRES USER APPROVAL | Pending |
| D-02 | Production image storage | Local private disk; volume vs private object storage | Chọn theo scale và restore ability | Storage service/deploy | Production deploy | OPEN — REQUIRES USER APPROVAL | Pending |
| D-03 | Hosting provider | None selected | Compare managed hosting options | DNS, secret, CI | Deploy | OPEN — REQUIRES USER APPROVAL | Pending |
| D-04 | Domain and HTTPS | None selected | HTTPS mandatory in production | URLs/certs | Deploy | OPEN — REQUIRES USER APPROVAL | Pending |
| D-05 | Mobile navigation | Options A/B/C in sitemap | Option C is lean MVP candidate | Flutter shell/UI | Albums mobile | OPEN — REQUIRES USER APPROVAL | Pending |
| D-06 | Album placement | No Flutter UI | Decide nav vs secondary | Routes/UI | Phase 19E | OPEN — REQUIRES USER APPROVAL | Pending |
| D-07 | Trash placement | No Flutter list API/UI | Decide after API design | API/UI | Trash phase | OPEN — REQUIRES USER APPROVAL | Pending |
| D-08 | Settings placement | No Flutter settings UI | Secondary navigation candidate | API/UI | Settings phase | OPEN — REQUIRES USER APPROVAL | Pending |
| D-09 | Image quota | Per Memory=10, per file=5MB; no user quota | Define storage quota | Validation/data | Release | OPEN — REQUIRES USER APPROVAL | Pending |
| D-10 | Resizing/thumbnails | Not implemented | Defer unless performance requires | Image pipeline | Scale/release | OPEN — REQUIRES USER APPROVAL | Pending |
| D-11 | EXIF removal | Not implemented | Decide privacy default before production | Image pipeline/policy | Production | OPEN — REQUIRES USER APPROVAL | Pending |
| D-12 | Backup frequency | Not defined | Set RPO/RTO and restore drill | Operations | Production | OPEN — REQUIRES USER APPROVAL | Pending |
| D-13 | Backup retention | Not defined | Define retention/versioning | Cost/privacy | Production | OPEN — REQUIRES USER APPROVAL | Pending |
| D-14 | Trash retention | No policy | Define period before permanent delete exists | Lifecycle/UI | Deletion work | OPEN — REQUIRES USER APPROVAL | Pending |
| D-15 | Permanent deletion | Not implemented | Specify user/admin behavior and backups | API/data/files | Deletion work | OPEN — REQUIRES USER APPROVAL | Pending |
| D-16 | Account deletion | Not implemented | Define export/delete sequence | Identity/data/files | Account phase | OPEN — REQUIRES USER APPROVAL | Pending |
| D-17 | Data export | Not implemented | Scope export format and access | API/job/privacy | Account phase | OPEN — REQUIRES USER APPROVAL | Pending |
| D-18 | Monitoring/crash reporting | Not selected | Select privacy-conscious tool later | Ops/mobile | Beta | OPEN — REQUIRES USER APPROVAL | Pending |
| D-19 | Production email provider | Development log/optional SMTP code | Choose provider and secrets process | Deliverability | Production | OPEN — REQUIRES USER APPROVAL | Pending |
| D-20 | CI/CD | Not selected | Add after deployment decision | Build/deploy | Beta | OPEN — REQUIRES USER APPROVAL | Pending |
| D-21 | Android minimum version | Not frozen in product docs | Set after device support review | Flutter config/testing | Mobile release | OPEN — REQUIRES USER APPROVAL | Pending |
| D-22 | iOS roadmap | Source exists; unverified on Windows | Plan separate macOS/iOS QA | Capability/testing | iOS release | OPEN — REQUIRES USER APPROVAL | Pending |
| D-23 | Manual device QA | Android partial-success smoke pending | Complete on supported device before freeze | Release confidence | Before 19D freeze | OPEN — REQUIRES USER APPROVAL | Pending |
| D-24 | Memory cover | Nullable `Memory.CoverImageId`; null means automatic | Manual image must belong to the Memory; automatic uses first valid upload-order image | API/Flutter display | Phase 19E | APPROVED | Checkpoint 1 |
| D-25 | Album cover | Nullable `Album.CoverImageId`; null means automatic | Manual image must belong to active Album Memory; automatic uses newest `AlbumMemory.AddedAt` then effective Memory cover | API/Flutter display | Phase 19E | APPROVED | Checkpoint 1 |
| D-26 | Album cover upload | No separate cover upload | Existing private Memory image only | Storage/privacy | Phase 19E | APPROVED | Checkpoint 1 |
