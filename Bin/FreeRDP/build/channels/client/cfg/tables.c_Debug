/**
 * FreeRDP: A Remote Desktop Protocol Implementation
 * Static Entry Point Tables
 *
 * Copyright 2012 Marc-Andre Moreau <marcandre.moreau@gmail.com>
 * Copyright 2015 Thincast Technologies GmbH
 * Copyright 2015 DI (FH) Martin Haimberger <martin.haimberger@thincast.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#include <freerdp/dvc.h>
#include <freerdp/channels/rdpdr.h>
#include "tables.h"




extern BOOL VCAPITYPE drdynvc_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);
extern BOOL VCAPITYPE remdesk_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);
extern BOOL VCAPITYPE rdpsnd_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);
extern BOOL VCAPITYPE rdpdr_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);
extern BOOL VCAPITYPE rail_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);
extern BOOL VCAPITYPE encomsp_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);
extern BOOL VCAPITYPE cliprdr_VirtualChannelEntryEx(PCHANNEL_ENTRY_POINTS_EX,PVOID);

extern UINT VCAPITYPE video_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE urbdrc_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE rdpsnd_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE rdpgfx_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE rdpei_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE location_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE geometry_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE echo_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE disp_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE audin_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);
extern UINT VCAPITYPE ainput_DVCPluginEntry(IDRDYNVC_ENTRY_POINTS* pEntryPoints);

extern UINT VCAPITYPE smartcard_DeviceServiceEntry(PDEVICE_SERVICE_ENTRY_POINTS pEntryPoints);
extern UINT VCAPITYPE printer_DeviceServiceEntry(PDEVICE_SERVICE_ENTRY_POINTS pEntryPoints);
extern UINT VCAPITYPE drive_DeviceServiceEntry(PDEVICE_SERVICE_ENTRY_POINTS pEntryPoints);

extern UINT VCAPITYPE libusb_freerdp_urbdrc_client_subsystem_entry(void*);
extern UINT VCAPITYPE winmm_freerdp_rdpsnd_client_subsystem_entry(void*);
extern UINT VCAPITYPE fake_freerdp_rdpsnd_client_subsystem_entry(void*);
extern UINT VCAPITYPE win_freerdp_printer_client_subsystem_entry(void*);
extern UINT VCAPITYPE winmm_freerdp_audin_client_subsystem_entry(void*);

extern const STATIC_ENTRY_VCEX CLIENT_VirtualChannelEntryEx_TABLE[];
const STATIC_ENTRY_VCEX CLIENT_VirtualChannelEntryEx_TABLE[] =
{

	{ "drdynvc", drdynvc_VirtualChannelEntryEx },
	{ "remdesk", remdesk_VirtualChannelEntryEx },
	{ "rdpsnd", rdpsnd_VirtualChannelEntryEx },
	{ "rdpdr", rdpdr_VirtualChannelEntryEx },
	{ "rail", rail_VirtualChannelEntryEx },
	{ "encomsp", encomsp_VirtualChannelEntryEx },
	{ "cliprdr", cliprdr_VirtualChannelEntryEx },
	{ nullptr, nullptr }
};
extern const STATIC_ENTRY_DVC CLIENT_DVCPluginEntry_TABLE[];
const STATIC_ENTRY_DVC CLIENT_DVCPluginEntry_TABLE[] =
{

	{ "video", video_DVCPluginEntry },
	{ "urbdrc", urbdrc_DVCPluginEntry },
	{ "rdpsnd", rdpsnd_DVCPluginEntry },
	{ "rdpgfx", rdpgfx_DVCPluginEntry },
	{ "rdpei", rdpei_DVCPluginEntry },
	{ "location", location_DVCPluginEntry },
	{ "geometry", geometry_DVCPluginEntry },
	{ "echo", echo_DVCPluginEntry },
	{ "disp", disp_DVCPluginEntry },
	{ "audin", audin_DVCPluginEntry },
	{ "ainput", ainput_DVCPluginEntry },
	{ nullptr, nullptr }
};
extern const STATIC_ENTRY_DSE CLIENT_DeviceServiceEntry_TABLE[];
const STATIC_ENTRY_DSE CLIENT_DeviceServiceEntry_TABLE[] =
{

	{ "smartcard", smartcard_DeviceServiceEntry },
	{ "printer", printer_DeviceServiceEntry },
	{ "drive", drive_DeviceServiceEntry },
	{ nullptr, nullptr }
};

extern const STATIC_ENTRY_TABLE CLIENT_STATIC_ENTRY_TABLES[];
const STATIC_ENTRY_TABLE CLIENT_STATIC_ENTRY_TABLES[] =
{
	{ "VirtualChannelEntryEx", { .csevcex = CLIENT_VirtualChannelEntryEx_TABLE } },
	{ "DVCPluginEntry", { .csedvc = CLIENT_DVCPluginEntry_TABLE } },
	{ "DeviceServiceEntry", { .csedse = CLIENT_DeviceServiceEntry_TABLE } },
	{ nullptr, { .cse = nullptr } }
};

extern const STATIC_SUBSYSTEM_ENTRY CLIENT_DRDYNVC_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_DRDYNVC_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_VIDEO_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_VIDEO_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_URBDRC_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_URBDRC_SUBSYSTEM_TABLE[] =
{
	{ "libusb", "", libusb_freerdp_urbdrc_client_subsystem_entry },
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_SMARTCARD_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_SMARTCARD_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_REMDESK_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_REMDESK_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPSND_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPSND_SUBSYSTEM_TABLE[] =
{
	{ "winmm", "", winmm_freerdp_rdpsnd_client_subsystem_entry },
	{ "fake", "", fake_freerdp_rdpsnd_client_subsystem_entry },
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPGFX_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPGFX_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPEI_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPEI_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPDR_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_RDPDR_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_RAIL_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_RAIL_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_PRINTER_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_PRINTER_SUBSYSTEM_TABLE[] =
{
	{ "win", "", win_freerdp_printer_client_subsystem_entry },
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_LOCATION_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_LOCATION_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_GEOMETRY_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_GEOMETRY_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_ENCOMSP_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_ENCOMSP_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_ECHO_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_ECHO_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_DRIVE_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_DRIVE_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_DISP_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_DISP_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_CLIPRDR_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_CLIPRDR_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_AUDIN_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_AUDIN_SUBSYSTEM_TABLE[] =
{
	{ "winmm", "", winmm_freerdp_audin_client_subsystem_entry },
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_SUBSYSTEM_ENTRY CLIENT_AINPUT_SUBSYSTEM_TABLE[];
const STATIC_SUBSYSTEM_ENTRY CLIENT_AINPUT_SUBSYSTEM_TABLE[] =
{
	{ nullptr, nullptr, nullptr }
};
extern const STATIC_ADDIN_TABLE CLIENT_STATIC_ADDIN_TABLE[];
const STATIC_ADDIN_TABLE CLIENT_STATIC_ADDIN_TABLE[] =
{
	{ "drdynvc", "VirtualChannelEntryEx", { .csevcex = drdynvc_VirtualChannelEntryEx }, CLIENT_DRDYNVC_SUBSYSTEM_TABLE },
	{ "video", "DVCPluginEntry", { .csedvc = video_DVCPluginEntry }, CLIENT_VIDEO_SUBSYSTEM_TABLE },
	{ "urbdrc", "DVCPluginEntry", { .csedvc = urbdrc_DVCPluginEntry }, CLIENT_URBDRC_SUBSYSTEM_TABLE },
	{ "smartcard", "DeviceServiceEntry", { .csedse = smartcard_DeviceServiceEntry }, CLIENT_SMARTCARD_SUBSYSTEM_TABLE },
	{ "remdesk", "VirtualChannelEntryEx", { .csevcex = remdesk_VirtualChannelEntryEx }, CLIENT_REMDESK_SUBSYSTEM_TABLE },
	{ "rdpsnd", "VirtualChannelEntryEx", { .csevcex = rdpsnd_VirtualChannelEntryEx }, CLIENT_RDPSND_SUBSYSTEM_TABLE },
	{ "rdpsnd", "DVCPluginEntry", { .csedvc = rdpsnd_DVCPluginEntry }, CLIENT_RDPSND_SUBSYSTEM_TABLE },
	{ "rdpgfx", "DVCPluginEntry", { .csedvc = rdpgfx_DVCPluginEntry }, CLIENT_RDPGFX_SUBSYSTEM_TABLE },
	{ "rdpei", "DVCPluginEntry", { .csedvc = rdpei_DVCPluginEntry }, CLIENT_RDPEI_SUBSYSTEM_TABLE },
	{ "rdpdr", "VirtualChannelEntryEx", { .csevcex = rdpdr_VirtualChannelEntryEx }, CLIENT_RDPDR_SUBSYSTEM_TABLE },
	{ "rail", "VirtualChannelEntryEx", { .csevcex = rail_VirtualChannelEntryEx }, CLIENT_RAIL_SUBSYSTEM_TABLE },
	{ "printer", "DeviceServiceEntry", { .csedse = printer_DeviceServiceEntry }, CLIENT_PRINTER_SUBSYSTEM_TABLE },
	{ "location", "DVCPluginEntry", { .csedvc = location_DVCPluginEntry }, CLIENT_LOCATION_SUBSYSTEM_TABLE },
	{ "geometry", "DVCPluginEntry", { .csedvc = geometry_DVCPluginEntry }, CLIENT_GEOMETRY_SUBSYSTEM_TABLE },
	{ "encomsp", "VirtualChannelEntryEx", { .csevcex = encomsp_VirtualChannelEntryEx }, CLIENT_ENCOMSP_SUBSYSTEM_TABLE },
	{ "echo", "DVCPluginEntry", { .csedvc = echo_DVCPluginEntry }, CLIENT_ECHO_SUBSYSTEM_TABLE },
	{ "drive", "DeviceServiceEntry", { .csedse = drive_DeviceServiceEntry }, CLIENT_DRIVE_SUBSYSTEM_TABLE },
	{ "disp", "DVCPluginEntry", { .csedvc = disp_DVCPluginEntry }, CLIENT_DISP_SUBSYSTEM_TABLE },
	{ "cliprdr", "VirtualChannelEntryEx", { .csevcex = cliprdr_VirtualChannelEntryEx }, CLIENT_CLIPRDR_SUBSYSTEM_TABLE },
	{ "audin", "DVCPluginEntry", { .csedvc = audin_DVCPluginEntry }, CLIENT_AUDIN_SUBSYSTEM_TABLE },
	{ "ainput", "DVCPluginEntry", { .csedvc = ainput_DVCPluginEntry }, CLIENT_AINPUT_SUBSYSTEM_TABLE },
	{ nullptr, nullptr, { .cse = nullptr }, nullptr }
};

