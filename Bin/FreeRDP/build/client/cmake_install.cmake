# Install script for directory: C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/client

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/Compiled")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Release")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "FALSE")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/pkgconfig" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/freerdp-client3.pc")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3" TYPE FILE FILES
    "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/FreeRDP-ClientConfig.cmake"
    "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/FreeRDP-ClientConfigVersion.cmake"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3/FreeRDP-ClientTargets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3/FreeRDP-ClientTargets.cmake"
         "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/CMakeFiles/Export/9319b9a9c1f9ee5302d9267486d3b164/FreeRDP-ClientTargets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3/FreeRDP-ClientTargets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3/FreeRDP-ClientTargets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/CMakeFiles/Export/9319b9a9c1f9ee5302d9267486d3b164/FreeRDP-ClientTargets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/CMakeFiles/Export/9319b9a9c1f9ee5302d9267486d3b164/FreeRDP-ClientTargets-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/CMakeFiles/Export/9319b9a9c1f9ee5302d9267486d3b164/FreeRDP-ClientTargets-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/CMakeFiles/Export/9319b9a9c1f9ee5302d9267486d3b164/FreeRDP-ClientTargets-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/cmake/FreeRDP-Client3" TYPE FILE FILES "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/CMakeFiles/Export/9319b9a9c1f9ee5302d9267486d3b164/FreeRDP-ClientTargets-release.cmake")
  endif()
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for each subdirectory.
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/common/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/Windows/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/SDL/cmake_install.cmake")

endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/client/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
