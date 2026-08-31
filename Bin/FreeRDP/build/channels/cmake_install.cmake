# Install script for directory: C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/channels

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

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for each subdirectory.
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/drdynvc/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/video/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/urbdrc/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/telemetry/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/smartcard/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/serial/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/remdesk/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rdpsnd/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rdpgfx/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rdpemsc/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rdpei/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rdpecam/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rdpdr/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/rail/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/printer/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/parallel/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/location/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/geometry/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/encomsp/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/echo/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/drive/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/disp/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/cliprdr/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/audin/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/ainput/cmake_install.cmake")
  include("C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/client/cmake_install.cmake")

endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
if(CMAKE_INSTALL_LOCAL_ONLY)
  file(WRITE "C:/Users/Junnu/Freerdp Wrapper/Bin/FreeRDP/build/channels/install_local_manifest.txt"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
endif()
