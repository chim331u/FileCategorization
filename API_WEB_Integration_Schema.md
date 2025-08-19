# FileCategorization - API & Web Integration Schema

## Complete Mapping: Web Pages → Fluxor Actions → Effects/Services → API Endpoints

### 📋 Schema Overview

| **Web Page** | **UI Component** | **Fluxor Action** | **Effect Handler** | **Service Method** | **API Endpoint v2** | **HTTP Method** | **Description** |
|--------------|------------------|-------------------|--------------------|-------------------|---------------------|-----------------|-----------------|
| **FileCategorizationIndex.razor** | | | | | | | **Main File Management Interface** |
| `/filecategorizationindex` | Filter SelectBar | `LoadFilesAction(searchParam)` | `HandleLoadFilesAction` | `GetFileListAsync(searchParam)` | `/api/v2/files/filtered/{filterType}` | `GET` | Load files by filter (1=All, 2=Categorized, 3=ToCategorize) |
| | Refresh Button | `RefreshDataAction()` | `HandleRefreshDataAction` | `RefreshCategoryAsync()` | `/api/v2/actions/refresh-files` | `POST` | Scan directory and refresh file list with ML categorization |
| | Train Model Button | `TrainModelAction()` | `HandleTrainModelAction` | `TrainModelAsync()` | `/api/v2/actions/train-model` | `POST` | Train ML model and save to disk |
| | Force Categories Button | `ForceCategoryAction()` | `HandleForceCategoryAction` | `ForceCategoryAsync()` | `/api/v2/actions/force-categorize` | `POST` | Re-categorize uncategorized files using ML |
| | Move Files Button | `MoveFilesAction(filesToMove)` | `HandleMoveFilesAction` | `MoveFilesAsync(filesToMove)` | `/api/v2/actions/move-files` | `POST` | Move files to their categorized directories |
| | OnInitialized | `LoadCategoriesAction()` | `HandleLoadCategoriesAction` | `GetCategoryListAsync()` | `/api/v2/categories` | `GET` | Load all distinct file categories |
| | Category Dropdown | `UpdateFileDetailAction(file)` | `HandleUpdateFileDetailAction` | `UpdateFileDetailAsync(file)` | `/api/v2/files/{id}` | `PUT` | Update file details and category |
| | Not Show Again Button | `NotShowAgainFileAction(file)` | `HandleNotShowAgainFileAction` | `NotShowAgainAsync(fileId)` | `/api/v2/files/{id}/not-show-again` | `PATCH` | Mark file as not to show again |
| **Config.razor** | | | | | | | **Configuration Management** |
| `/config` | OnInitialized | `LoadConfigurationsAction()` | `HandleLoadConfigurationsAction` | `GetConfigListAsync()` | `/api/v2/configs` | `GET` | Load all configuration settings |
| | Add Config Button | `CreateConfigurationAction(config)` | `HandleCreateConfigurationAction` | `AddConfigAsync(config)` | `/api/v2/configs` | `POST` | Create new configuration entry |
| | Edit/Save Button | `UpdateConfigurationAction(config)` | `HandleUpdateConfigurationAction` | `UpdateConfigAsync(config)` | `/api/v2/configs/{id}` | `PUT` | Update existing configuration |
| | Delete Button | `DeleteConfigurationAction(config)` | `HandleDeleteConfigurationAction` | `DeleteConfigAsync(config)` | `/api/v2/configs/{id}` | `DELETE` | Soft delete configuration |
| **LastView.razor** | | | | | | | **Hierarchical File Browser** |
| `/lastview` | OnInitialized | `LoadLastViewFilesAction()` | `HandleLoadLastViewFilesAction` | `GetLastFilesListAsync()` | `/api/v2/files/lastview` | `GET` | Get latest file from each category |
| | Row Expansion | `LoadFilesByCategoryAction(category)` | `HandleLoadFilesByCategoryAction` | `GetAllFilesAsync(category)` | `/api/v2/files/category/{category}` | `GET` | Get all files for specific category |
| | Not Show Again Button | `NotShowAgainFileAction(file)` | `HandleNotShowAgainFileAction` | `NotShowAgainAsync(fileId)` | `/api/v2/files/{id}/not-show-again` | `PATCH` | Mark file as not to show again (removes from expanded list) |
| **WebScrum.razor** | | | | | | | **DownloadDaemon Interface (Direct Service Calls)** |
| `/WebScrum` | Load Threads | Direct Service Call | N/A | `GetActiveThreads()` | `/api/v2/dd/threads` | `GET` | Get all active DD threads with statistics |
| | Thread Click | Direct Service Call | N/A | `GetEd2kLinks(threadId)` | `/api/v2/dd/threads/{threadId}/links` | `GET` | Get ED2K links for specific thread |
| | Check URL Button | Direct Service Call | N/A | `CheckUrl(url)` | `/api/v2/dd/threads/process` | `POST` | Process DD thread URL and extract links |
| | Renew Thread | Direct Service Call | N/A | `RenewThread(threadId)` | `/api/v2/dd/threads/{threadId}/refresh` | `POST` | Refresh existing thread links |
| | Use Link Button | Direct Service Call | N/A | `UseLink(linkId)` | `/api/v2/dd/links/{linkId}/use` | `POST` | Mark ED2K link as used |

---

## 🔄 Real-Time Updates via SignalR

| **SignalR Event** | **Triggered By** | **Fluxor Action Dispatched** | **UI Updates** | **Pages Affected** |
|-------------------|------------------|------------------------------|----------------|-------------------|
| `moveFilesNotifications` | File move operations | `SignalRFileMovedAction` | File removed from grid | FileCategorizationIndex, LastView |
| `jobNotifications` | Background job updates | `SignalRJobCompletedAction` | Console messages | All pages with console |
| `categoryRefreshNotifications` | Category refresh | `SignalRCategoryRefreshAction` | Category list updated | FileCategorizationIndex |

---

## 📊 Additional API v2 Endpoints (Available but not directly used)

| **API Endpoint** | **HTTP Method** | **Description** | **Potential Usage** |
|------------------|-----------------|-----------------|-------------------|
| `/api/v2/configs/key/{key}` | `GET` | Get configuration by key | Direct config access |
| `/api/v2/configs/value/{key}` | `GET` | Get configuration value by key | Value-only retrieval |
| `/api/v2/configs/environment/{isDev}` | `GET` | Get configs by environment | Environment filtering |
| `/api/v2/files/search?pattern={pattern}` | `GET` | Search files by name pattern | Advanced search functionality |
| `/api/v2/files/tocategorize` | `GET` | Get files needing categorization | Specialized filtering |
| `/api/v2/files` | `POST` | Create new file record | File management |
| `/api/v2/files/move` | `PATCH` | Move single file to category | Individual file moves |
| `/api/v2/files/{id}` | `DELETE` | Soft delete file record | File removal |
| `/api/v2/crypto/encrypt` | `POST` | Encrypt string using AES | Security operations |
| `/api/v2/crypto/decrypt` | `POST` | Decrypt string using AES | Security operations |
| `/api/v2/crypto/hash` | `POST` | Generate SHA256 hash | Password operations |
| `/api/v2/crypto/verify` | `POST` | Verify text against hash | Authentication |
| `/api/v2/dd/threads/{threadId}` | `DELETE` | Deactivate DD thread | Thread management |
| `/api/v2/actions/jobs/{jobId}/status` | `GET` | Get background job status | Job monitoring |

---

## ⚠️ Legacy API v1 Endpoints (DEPRECATED)

| **Legacy v1 Endpoint** | **Modern v2 Replacement** | **Migration Status** |
|------------------------|---------------------------|---------------------|
| `/api/v1/RefreshFiles` | `/api/v2/actions/refresh-files` | ✅ MIGRATED |
| `/api/v1/MoveFiles` | `/api/v2/actions/move-files` | ✅ MIGRATED |
| `/api/v1/ForceCategory` | `/api/v2/actions/force-categorize` | ✅ MIGRATED |
| `/api/v1/TrainModel` | `/api/v2/actions/train-model` | ✅ MIGRATED |
| `/api/v1/GetConfigList` | `/api/v2/configs` | ✅ MIGRATED |
| `/api/v1/filesDetailList` | `/api/v2/files/filtered/1` | ✅ MIGRATED |
| `/api/v1/CategoryList` | `/api/v2/categories` | ✅ MIGRATED |
| `/api/v1/CheckLink/{link}` | `/api/v2/dd/threads/process` | ✅ MIGRATED |
| `/api/v1/GetActiveThreads` | `/api/v2/dd/threads` | ✅ MIGRATED |

**Note**: All v1 endpoints are marked as `[Obsolete]` with clear migration paths to v2 equivalents.

---

## 🔧 Architecture Patterns Summary

### **API Layer (FileCategorization_Api)**
- **Modern v2**: Repository Pattern + Result Pattern + FluentValidation + AutoMapper
- **Legacy v1**: Direct service calls (deprecated)
- **Background Jobs**: Hangfire for async operations
- **Real-time**: SignalR hub at `/notifications`
- **ML Integration**: ML.NET for file categorization
- **Database**: SQLite with Entity Framework Core

### **Web Layer (FileCategorization_Web)**
- **State Management**: Fluxor (Redux pattern) with 40+ actions
- **Caching Strategy**: Cache-first with tag-based invalidation
- **Real-time**: SignalR client with auto-reconnection
- **UI Framework**: Radzen Blazor components
- **HTTP Services**: Modern patterns with HttpClientFactory

### **Data Flow Architecture**
```
Web Page → User Action → Fluxor Action → Effect Handler → Service Method → API Endpoint → Repository → Database
         ↖                                                                                              ↗
           ← UI Updates ← State Reducer ← SignalR Events ← Background Jobs ←―――――――――――――――――――――――――――――
```

---

## 🎯 Key Integration Points

1. **FileCategorizationIndex**: Primary interface using 7 different API endpoints
2. **Config**: CRUD operations with full state management
3. **LastView**: Hierarchical browsing with expansion-based loading
4. **WebScrum**: Direct service integration for DD operations
5. **Global Console**: Real-time system feedback via SignalR integration

This schema demonstrates the comprehensive integration between the FileCategorization API and Web projects, showcasing modern architectural patterns, real-time capabilities, and systematic migration from legacy v1 to modern v2 endpoints.