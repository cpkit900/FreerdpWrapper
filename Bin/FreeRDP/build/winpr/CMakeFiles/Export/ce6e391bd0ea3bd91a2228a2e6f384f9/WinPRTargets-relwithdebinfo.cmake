#----------------------------------------------------------------
# Generated CMake target import file for configuration "RelWithDebInfo".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "winpr" for configuration "RelWithDebInfo"
set_property(TARGET winpr APPEND PROPERTY IMPORTED_CONFIGURATIONS RELWITHDEBINFO)
set_target_properties(winpr PROPERTIES
  IMPORTED_IMPLIB_RELWITHDEBINFO "${_IMPORT_PREFIX}/lib/winpr3.lib"
  IMPORTED_LINK_DEPENDENT_LIBRARIES_RELWITHDEBINFO "json-c::json-c"
  IMPORTED_LOCATION_RELWITHDEBINFO "${_IMPORT_PREFIX}/bin/winpr3.dll"
  )

list(APPEND _cmake_import_check_targets winpr )
list(APPEND _cmake_import_check_files_for_winpr "${_IMPORT_PREFIX}/lib/winpr3.lib" "${_IMPORT_PREFIX}/bin/winpr3.dll" )

# Import target "winpr-makecert" for configuration "RelWithDebInfo"
set_property(TARGET winpr-makecert APPEND PROPERTY IMPORTED_CONFIGURATIONS RELWITHDEBINFO)
set_target_properties(winpr-makecert PROPERTIES
  IMPORTED_LOCATION_RELWITHDEBINFO "${_IMPORT_PREFIX}/bin/winpr-makecert.exe"
  )

list(APPEND _cmake_import_check_targets winpr-makecert )
list(APPEND _cmake_import_check_files_for_winpr-makecert "${_IMPORT_PREFIX}/bin/winpr-makecert.exe" )

# Import target "winpr-hash" for configuration "RelWithDebInfo"
set_property(TARGET winpr-hash APPEND PROPERTY IMPORTED_CONFIGURATIONS RELWITHDEBINFO)
set_target_properties(winpr-hash PROPERTIES
  IMPORTED_LOCATION_RELWITHDEBINFO "${_IMPORT_PREFIX}/bin/winpr-hash.exe"
  )

list(APPEND _cmake_import_check_targets winpr-hash )
list(APPEND _cmake_import_check_files_for_winpr-hash "${_IMPORT_PREFIX}/bin/winpr-hash.exe" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
