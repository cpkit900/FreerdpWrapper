#----------------------------------------------------------------
# Generated CMake target import file for configuration "Debug".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "freerdp-client" for configuration "Debug"
set_property(TARGET freerdp-client APPEND PROPERTY IMPORTED_CONFIGURATIONS DEBUG)
set_target_properties(freerdp-client PROPERTIES
  IMPORTED_IMPLIB_DEBUG "${_IMPORT_PREFIX}/lib/freerdp-client3.lib"
  IMPORTED_LOCATION_DEBUG "${_IMPORT_PREFIX}/bin/freerdp-client3.dll"
  )

list(APPEND _cmake_import_check_targets freerdp-client )
list(APPEND _cmake_import_check_files_for_freerdp-client "${_IMPORT_PREFIX}/lib/freerdp-client3.lib" "${_IMPORT_PREFIX}/bin/freerdp-client3.dll" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
