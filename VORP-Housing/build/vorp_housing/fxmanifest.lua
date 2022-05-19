game 'rdr3'

fx_version 'cerulean'
rdr3_warning 'I acknowledge that this is a prerelease build of RedM, and I am aware my resources *will* become incompatible once RedM ships.'

client_scripts { 'vorphousing_cl.net.dll' }
server_scripts { 'vorphousing_sv.net.dll' }

files { 'Newtonsoft.Json.dll' }

-- Log Levels; none, trace, debug, info, warn, error, all
log_level 'none'

--dont touch
version '1.0'
vorp_checker 'yes'
vorp_name '^4Resource version Check^3'
vorp_github 'https://github.com/VORPCORE/VORP-Housing'
