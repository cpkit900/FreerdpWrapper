#----------------------------------------------------------------
# Generated CMake target import file for configuration "RelWithDebInfo".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "freerdp" for configuration "RelWithDebInfo"
set_property(TARGET freerdp APPEND PROPERTY IMPORTED_CONFIGURATIONS RELWITHDEBINFO)
set_target_properties(freerdp PROPERTIES
  IMPORTED_IMPLIB_RELWITHDEBINFO "${_IMPORT_PREFIX}/lib/freerdp3.lib"
  IMPORTED_LOCATION_RELWITHDEBINFO "${_IMPORT_PREFIX}/bin/freerdp3.dll"
  )

list(APPEND _cmake_import_check_targets freerdp )
list(APPEND _cmake_import_check_files_for_freerdp "${_IMPORT_PREFIX}/lib/freerdp3.lib" "${_IMPORT_PREFIX}/bin/freerdp3.dll" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
