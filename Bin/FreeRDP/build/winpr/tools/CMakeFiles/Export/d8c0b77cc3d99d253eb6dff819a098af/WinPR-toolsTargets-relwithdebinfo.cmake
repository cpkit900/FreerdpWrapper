#----------------------------------------------------------------
# Generated CMake target import file for configuration "RelWithDebInfo".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "winpr-tools" for configuration "RelWithDebInfo"
set_property(TARGET winpr-tools APPEND PROPERTY IMPORTED_CONFIGURATIONS RELWITHDEBINFO)
set_target_properties(winpr-tools PROPERTIES
  IMPORTED_IMPLIB_RELWITHDEBINFO "${_IMPORT_PREFIX}/lib/winpr-tools3.lib"
  IMPORTED_LOCATION_RELWITHDEBINFO "${_IMPORT_PREFIX}/bin/winpr-tools3.dll"
  )

list(APPEND _cmake_import_check_targets winpr-tools )
list(APPEND _cmake_import_check_files_for_winpr-tools "${_IMPORT_PREFIX}/lib/winpr-tools3.lib" "${_IMPORT_PREFIX}/bin/winpr-tools3.dll" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
