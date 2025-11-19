# LingapDVO Data Seeder

## Overview

This data seeder generates **250 dummy applications** with realistic Filipino names, addresses, and encrypted ID files for testing and development purposes.

## Features

### Generated Data
- **250 random applications** distributed across:
  - Hospital Assistance
  - Funeral Assistance
  - Other Assistance (Medical Procedures/Lab Tests)

### Realistic Data Points
- **Filipino Names**: Authentic Filipino first names, middle names, and last names
- **Davao City Addresses**: Real barangays and districts from Davao City
- **Multiple ID Types**: National ID, Driver's License, and UMID
- **Encrypted Files**: All files encrypted using AES-256-CBC with PKCS7 padding
- **Complete Workflow**: Applications in various states (Waiting, Processing, Approved, Rejected, Claimed)

### Encrypted Files Generated
All files are stored in the system's designated folders:
1. **Valid ID (Front)** - Encrypted with AES-256 (stored in `wwwroot/Validimg`)
2. **Valid ID (Back)** - Encrypted with AES-256 (stored in `wwwroot/Validimg`)
3. **Doctor Prescriptions** - For hospital/medical assistance (stored in respective FileStorage folders)
4. **Death Certificates** - For funeral assistance (stored in `wwwroot/FuneralAssistanceFileStorage`)
5. **Medical Certificates** - For medical procedure assistance (stored in `wwwroot/OtherAssistanceFileStorage`)

## Encryption Details

- **Algorithm**: AES-256-CBC
- **Padding**: PKCS7
- **Key**: `2B7E151628AED2A6ABF7158809CF4F3C762E7160F38B4DA56A784D904519033C`
- **IV**: Randomly generated for each file (stored in first 16 bytes)
- **File Format**: `[16-byte IV][Encrypted Data]`

## How to Use

### Method 1: Via Web Browser (Recommended)

1. **Start the application** in Development mode
2. **Access the seeder endpoint**:
   ```
   http://localhost:5000/DataSeeder/Seed
   ```
3. **Wait for completion** - You'll see a success message with details

### Method 2: Via API Call

```bash
curl http://localhost:5000/DataSeeder/Seed
```

### Check Statistics

To see how many applications were created:
```
http://localhost:5000/DataSeeder/Stats
```

Response example:
```json
{
  "success": true,
  "statistics": {
    "total": 250,
    "hospitalAssistance": 85,
    "funeralAssistance": 82,
    "otherAssistance": 83,
    "environment": "Development"
  }
}
```

### Clear All Seeded Data

**⚠️ WARNING: This will delete ALL applications and encrypted files!**

```
http://localhost:5000/DataSeeder/Clear
```

## Data Distribution

The seeder creates a realistic distribution of:

### Form Types
- **~33%** Hospital Assistance applications
- **~33%** Funeral Assistance applications
- **~33%** Other Assistance applications

### Application States
- **Waiting** (not yet processed)
- **Processing** (being reviewed)
- **Approved** (decision made)
- **Pending** (additional information needed)
- **Rejected** (denied)
- **Claimed** (approved and claimed by applicant)

### Time Distribution
- Applications created over the **last 60 days**
- Some applications are **archived** (older than 30 days)
- Realistic processing times (2-48 hours after submission)
- Approval delays (1-7 days for decision)
- Claim delays (7-14 days after approval)

## Sample Data

### Names
- First Names: Maria, Jose, Juan, Ana, Pedro, Rosa, Miguel, Carmen...
- Last Names: Santos, Reyes, Cruz, Bautista, Ocampo, Garcia, Mendoza...
- Suffixes: Jr., Sr., II, III (10% probability)

### Addresses
- Real Davao City barangays: Acacia, Agdao, Alambre, Catalunan Grande...
- Real Davao districts: Poblacion, Agdao, Buhangin, Calinan, Marilog...
- Street addresses: "123 Main St", "45 Roxas Ave", "67 Bonifacio St"...

### Contact Information
- Phone numbers: 09XXXXXXXXX (valid Philippine mobile format)
- PhilHealth: 50% have PhilHealth numbers

### Assistance Types

**Hospital Assistance:**
- Medicine
- Laboratory
- Hospital Bill
- Medical Supplies
- Dialysis
- Chemotherapy

**Funeral Assistance:**
- Burial Assistance
- Funeral Services
- Casket
- Memorial Services

**Other Assistance:**
- Laboratory Test
- Medical Procedure
- X-Ray
- Ultrasound
- CT Scan
- MRI

## File Locations

After running the seeder, encrypted files are stored in the system's designated folders:

```
wwwroot/
├── Validimg/                           # All encrypted Valid ID front and back images (500 files)
├── HospitalAssistanceFileStorage/      # Hospital prescriptions and death certificates
├── FuneralAssistanceFileStorage/       # Funeral death certificates
└── OtherAssistanceFileStorage/         # Medical procedure prescriptions and medical certificates
```

### File Distribution by Folder:
- **Validimg**: ~500 encrypted ID files (250 front + 250 back) for all form types
- **HospitalAssistanceFileStorage**: ~83 prescription files for hospital assistance applications
- **FuneralAssistanceFileStorage**: ~83 death certificate files for funeral assistance applications
- **OtherAssistanceFileStorage**: ~168 files (~84 prescriptions + ~84 medical certificates) for medical procedure applications

## Safety Features

1. **Development Only**: Seeder only runs in Development environment
2. **Confirmation Required**: Clear operation warns before deletion
3. **Logging**: All operations are logged for audit trail
4. **Transaction Safety**: Uses database transactions to prevent partial data

## Technical Implementation

### DataSeeder Class
- **Location**: `Services/DataSeeder.cs`
- **Dependencies**: ApplicationDbContext, IWebHostEnvironment, IDateTimeService
- **Methods**:
  - `SeedDataAsync()` - Main seeding method
  - `GeneratePersonData()` - Creates realistic person information
  - `CreateHospitalAssistance()` - Generates hospital application
  - `CreateFuneralAssistance()` - Generates funeral application
  - `CreateOtherAssistance()` - Generates medical procedure application
  - `CreateEncryptedIdFile()` - Creates encrypted ID files
  - `EncryptFileBytes()` - Encrypts file data using AES-256

### DataSeederController
- **Location**: `Controllers/DataSeederController.cs`
- **Endpoints**:
  - `GET /DataSeeder/Seed` - Run the seeder
  - `GET /DataSeeder/Clear` - Clear all data
  - `GET /DataSeeder/Stats` - Get statistics

## Verification

After seeding, verify the data:

### Check Database
```sql
-- Check total applications
SELECT
    (SELECT COUNT(*) FROM HospitalAssistance) AS Hospital,
    (SELECT COUNT(*) FROM FuneralAssistance) AS Funeral,
    (SELECT COUNT(*) FROM OtherAssistance) AS Other,
    (SELECT COUNT(*) FROM HospitalAssistance) +
    (SELECT COUNT(*) FROM FuneralAssistance) +
    (SELECT COUNT(*) FROM OtherAssistance) AS Total;
```

### Check Files
```bash
# Count encrypted ID files (should be 500: 250 front + 250 back)
ls -1 wwwroot/Validimg/*.enc | wc -l

# Count hospital assistance files (should be ~83 prescriptions)
ls -1 wwwroot/HospitalAssistanceFileStorage/*.enc | wc -l

# Count funeral assistance files (should be ~83 death certificates)
ls -1 wwwroot/FuneralAssistanceFileStorage/*.enc | wc -l

# Count other assistance files (should be ~168: ~84 prescriptions + ~84 medical certificates)
ls -1 wwwroot/OtherAssistanceFileStorage/*.enc | wc -l
```

**Windows PowerShell:**
```powershell
# Count encrypted ID files
(Get-ChildItem wwwroot\Validimg\*.enc).Count

# Count hospital assistance files
(Get-ChildItem wwwroot\HospitalAssistanceFileStorage\*.enc).Count

# Count funeral assistance files
(Get-ChildItem wwwroot\FuneralAssistanceFileStorage\*.enc).Count

# Count other assistance files
(Get-ChildItem wwwroot\OtherAssistanceFileStorage\*.enc).Count
```

### Verify Encryption
The encrypted files should:
- Start with a 16-byte IV
- Be larger than the original (due to IV and padding)
- Not be readable as plain text
- Decrypt properly using the system's decrypt function

## Troubleshooting

### Error: "Data seeding is only allowed in development environment"
**Solution**: Ensure `ASPNETCORE_ENVIRONMENT=Development` in your launch settings

### Error: "Cannot access AES key"
**Solution**: Verify the AES key is correctly set in the seeder class

### Files not created
**Solution**: Check that the wwwroot directory exists and has write permissions. The seeder will automatically create the required subdirectories (Validimg, HospitalAssistanceFileStorage, FuneralAssistanceFileStorage, OtherAssistanceFileStorage)

### Database constraint errors
**Solution**: Ensure user accounts exist (UserId 1-50) before running the seeder

## Notes

1. **Performance**: Seeding 250 applications takes approximately 10-30 seconds
2. **Disk Space**: Encrypted files require approximately 5-10 MB total
3. **Database Size**: 250 applications add approximately 1-2 MB to database
4. **Idempotency**: Running the seeder multiple times will create duplicate data
5. **Cleanup**: Use the `/DataSeeder/Clear` endpoint to remove all seeded data before re-seeding

## Security Considerations

1. **Remove in Production**: Delete or disable the DataSeederController in production
2. **Access Control**: Add authentication/authorization if needed
3. **AES Key**: The key is for testing only; use secure key management in production
4. **File Paths**: Validate all file paths to prevent directory traversal

## License

This seeder is part of the LingapDVO project and follows the same license.
